using System.Collections.Generic;
using UnityEngine;

namespace ReactorTechnician
{
    public sealed class CoolantPipeNetworkBuilder : MonoBehaviour
    {
        [SerializeField] private WaveFunctionLevelGenerator levelGenerator;
        [SerializeField] private CoolingFlowPath mainFlow;
        [SerializeField] private CoolingFlowPath auxiliaryFlow;
        [SerializeField] private Material inactiveMaterial;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private float pipeHeight = 0.32f;
        [SerializeField] private float pipeRadius = 0.14f;
        [SerializeField] private float jointRadius = 0.24f;

        private readonly List<Renderer> mainRenderers = new List<Renderer>();
        private readonly List<Renderer> auxiliaryRenderers = new List<Renderer>();

        private void Start()
        {
            RebuildNetwork();
        }

        public void RebuildNetwork()
        {
            ResolveReferences();
            Clear();
            mainRenderers.Clear();
            auxiliaryRenderers.Clear();

            Vector3 reactor = WithPipeHeight(GetGeneratedPoint("ReactorHall", Vector3.zero));
            Vector3 valveRoom = WithPipeHeight(GetGeneratedPoint("ValveRoom", new Vector3(12f, 0f, 0f)));
            Vector3 storage = WithPipeHeight(GetGeneratedPoint("ModuleStorage", new Vector3(-12f, 0f, -7f)));
            Vector3 overheat = WithPipeHeight(GetGeneratedPoint("OverheatRoom", new Vector3(14f, 0f, 14f)));
            Vector3 terminal = WithPipeHeight(GetGeneratedPoint("TerminalRoom", new Vector3(0f, 0f, 14f)));

            BuildPolyline("Main Coolant Loop", mainRenderers, reactor, valveRoom, overheat, terminal, reactor + new Vector3(0f, 0f, 2.6f));
            BuildPolyline("Module Supply Branch", auxiliaryRenderers, storage, reactor + new Vector3(-2.5f, 0f, 2.6f), reactor + new Vector3(2.5f, 0f, 2.6f));

            mainFlow?.ConfigurePipeRenderers(mainRenderers.ToArray());
            auxiliaryFlow?.ConfigurePipeRenderers(auxiliaryRenderers.ToArray());
        }

        private Vector3 GetGeneratedPoint(string key, Vector3 fallbackOffset)
        {
            if (levelGenerator != null && levelGenerator.HasValidLayout)
            {
                return levelGenerator.GetGeneratedPoint(key);
            }

            return transform.position + fallbackOffset;
        }

        private Vector3 WithPipeHeight(Vector3 position)
        {
            position.y = pipeHeight;
            return position;
        }

        private void BuildPolyline(string label, List<Renderer> renderers, params Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                AddJoint($"{label} Joint {i + 1}", points[i], renderers);
                AddPipeSupport($"{label} Support {i + 1}", points[i]);
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                AddPipeSegment($"{label} Segment {i + 1}", points[i], points[i + 1], renderers);
            }
        }

        private void AddPipeSegment(string objectName, Vector3 start, Vector3 end, List<Renderer> renderers)
        {
            Vector3 delta = end - start;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }

            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = objectName;
            pipe.transform.SetParent(transform, false);
            pipe.transform.position = (start + end) * 0.5f;
            pipe.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            pipe.transform.localScale = new Vector3(pipeRadius, delta.magnitude * 0.5f, pipeRadius);
            DisableCollider(pipe);

            Renderer renderer = pipe.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = inactiveMaterial;
                renderers.Add(renderer);
            }
        }

        private void AddJoint(string objectName, Vector3 position, List<Renderer> renderers)
        {
            GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            joint.name = objectName;
            joint.transform.SetParent(transform, false);
            joint.transform.position = position;
            joint.transform.localScale = Vector3.one * jointRadius;
            DisableCollider(joint);

            Renderer renderer = joint.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = inactiveMaterial;
                renderers.Add(renderer);
            }
        }

        private void AddPipeSupport(string objectName, Vector3 position)
        {
            GameObject support = GameObject.CreatePrimitive(PrimitiveType.Cube);
            support.name = objectName;
            support.transform.SetParent(transform, false);
            support.transform.position = new Vector3(position.x, 0.08f, position.z);
            support.transform.localScale = new Vector3(0.75f, 0.16f, 0.75f);
            DisableCollider(support);
            Renderer renderer = support.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = activeMaterial != null ? activeMaterial : inactiveMaterial;
            }
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private void ResolveReferences()
        {
            if (levelGenerator == null)
            {
                levelGenerator = FindAnyObjectByType<WaveFunctionLevelGenerator>();
            }

            if (mainFlow == null || auxiliaryFlow == null)
            {
                CoolingFlowPath[] flows = FindObjectsByType<CoolingFlowPath>(FindObjectsInactive.Exclude);
                for (int i = 0; i < flows.Length; i++)
                {
                    if (mainFlow == null)
                    {
                        mainFlow = flows[i];
                    }
                    else if (auxiliaryFlow == null && flows[i] != mainFlow)
                    {
                        auxiliaryFlow = flows[i];
                    }
                }
            }
        }

        private static void DisableCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
}
