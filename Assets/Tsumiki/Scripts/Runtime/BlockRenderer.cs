using System.Collections.Generic;
using System.Linq;
using Tsumiki.Core;
using UnityEngine;

namespace Tsumiki.Runtime
{
    public sealed class BlockRenderer : MonoBehaviour
    {
        private readonly List<GameObject> cubes = new();
        private Material[] materials;

        private void Awake()
        {
            materials = new Material[TsumikiPalette.Blocks.Length];
            // This unlit shader keeps every block face at the exact same color when
            // the model rotates; directional lighting must not recolor the blocks.
            var shader = Resources.Load<Shader>("Shaders/TsumikiFlatColor");
            if (!shader) shader = Shader.Find("Sprites/Default");
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(shader) { color = TsumikiPalette.Blocks[i] };
                if (materials[i].HasProperty("_BaseColor")) materials[i].SetColor("_BaseColor", TsumikiPalette.Blocks[i]);
                if (materials[i].HasProperty("_Color")) materials[i].SetColor("_Color", TsumikiPalette.Blocks[i]);
                materials[i].SetFloat("_Smoothness", 0f);
            }
        }

        public void Show(HeightMap map, bool hideSome = false)
        {
            Clear();
            var center = new Vector3((map.Width - 1) * .5f, 0, (map.Depth - 1) * .5f);
            var blocks = map.Blocks().ToList();
            var hiddenBlocks = new HashSet<GridPosition>();
            if (hideSome && blocks.Count > 0)
            {
                var hideCount = Mathf.Max(1, Mathf.CeilToInt(blocks.Count / 3f));
                // Prefer blocks already behind others, then hide high blocks so the
                // change is still obvious even for a simple one-column stack.
                foreach (var block in blocks
                    .OrderByDescending(map.IsHidden)
                    .ThenByDescending(value => value.Z)
                    .ThenByDescending(value => value.X + value.Y)
                    .Take(hideCount))
                    hiddenBlocks.Add(block);
            }

            foreach (var block in blocks)
            {
                if (hiddenBlocks.Contains(block)) continue;
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"つみき {block}";
                cube.transform.SetParent(transform, false);
                cube.transform.localPosition = new Vector3(block.X, block.Z + .5f, block.Y) - center;
                cube.transform.localScale = Vector3.one * .98f;
                cube.AddComponent<BlockCell>().Set(block.X, block.Y);
                // Every face-adjacent cube receives a different one of the five colors.
                var index = (block.X + block.Y * 2 + block.Z * 3) % materials.Length;
                var renderer = cube.GetComponent<Renderer>();
                renderer.sharedMaterial = materials[index];
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                AddEdges(cube.transform);
                cubes.Add(cube);
            }
        }

        public void Clear()
        {
            foreach (var cube in cubes) if (cube) Destroy(cube);
            cubes.Clear();
        }

        private static void AddEdges(Transform parent)
        {
            var points = new[]
            {
                new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f), new Vector3(-.5f,-.5f,.5f),
                new Vector3(-.5f,.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f)
            };
            var pairs = new[] {0,1,1,2,2,3,3,0,4,5,5,6,6,7,7,4,0,4,1,5,2,6,3,7};
            for (var i = 0; i < pairs.Length; i += 2)
            {
                var edge = new GameObject("edge").AddComponent<LineRenderer>();
                edge.transform.SetParent(parent, false);
                edge.useWorldSpace = false; edge.positionCount = 2;
                edge.SetPositions(new[] { points[pairs[i]], points[pairs[i + 1]] });
                edge.startWidth = edge.endWidth = .052f;
                edge.material = new Material(Shader.Find("Sprites/Default")); edge.startColor = edge.endColor = TsumikiPalette.Outline;
            }
        }
    }
}
