using UnityEngine;

namespace ReactorTechnician
{
    public sealed class DynamicLevelVariation : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private bool randomizeSeed = true;
        [SerializeField] private Transform[] decorationAnchors;
        [SerializeField] private Material crateMaterial;
        [SerializeField] private Material coolantMaterial;
        [SerializeField] private Material warningMaterial;

        private enum TileKind
        {
            Empty,
            Crate,
            CoolantCanister,
            WarningPost
        }

        private void Start()
        {
            int resolvedSeed = randomizeSeed ? System.Environment.TickCount : seed;
            Random.InitState(resolvedSeed);
            GenerateDecorations();
        }

        private void GenerateDecorations()
        {
            if (decorationAnchors == null)
            {
                return;
            }

            TileKind previous = TileKind.Empty;
            for (int i = 0; i < decorationAnchors.Length; i++)
            {
                Transform anchor = decorationAnchors[i];
                if (anchor == null)
                {
                    continue;
                }

                TileKind next = PickTile(previous);
                previous = next;
                CreateTile(next, anchor, i);
            }
        }

        private static TileKind PickTile(TileKind previous)
        {
            float roll = Random.value;
            if (previous == TileKind.Crate)
            {
                roll += 0.18f;
            }

            if (roll < 0.35f) return TileKind.Empty;
            if (roll < 0.62f) return TileKind.Crate;
            if (roll < 0.82f) return TileKind.CoolantCanister;
            return TileKind.WarningPost;
        }

        private void CreateTile(TileKind kind, Transform anchor, int index)
        {
            switch (kind)
            {
                case TileKind.Crate:
                    CreatePrimitive($"Dynamic Crate {index + 1}", PrimitiveType.Cube, anchor, new Vector3(0.8f, 0.55f, 0.8f), crateMaterial);
                    break;
                case TileKind.CoolantCanister:
                    CreatePrimitive($"Dynamic Coolant Canister {index + 1}", PrimitiveType.Cylinder, anchor, new Vector3(0.32f, 0.65f, 0.32f), coolantMaterial);
                    break;
                case TileKind.WarningPost:
                    CreatePrimitive($"Dynamic Warning Post {index + 1}", PrimitiveType.Cylinder, anchor, new Vector3(0.16f, 0.9f, 0.16f), warningMaterial);
                    break;
            }
        }

        private static void CreatePrimitive(string objectName, PrimitiveType primitiveType, Transform anchor, Vector3 scale, Material material)
        {
            GameObject prop = GameObject.CreatePrimitive(primitiveType);
            prop.name = objectName;
            prop.transform.SetParent(anchor);
            prop.transform.localPosition = Vector3.zero;
            prop.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            prop.transform.localScale = scale;

            Collider propCollider = prop.GetComponent<Collider>();
            if (propCollider != null)
            {
                propCollider.isTrigger = true;
            }

            Renderer renderer = prop.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
