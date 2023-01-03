using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FloatingOriginAdvanced : MonoBehaviour
{
    private const float DEFAULT_DISTANCE = 25000.0f;

    public float distance = DEFAULT_DISTANCE;
    public bool checkParticles = true;
    public bool checkPhysics = true;
    public float physicsDistance = DEFAULT_DISTANCE;
    public float defaultSleepThreshold = 0.14f;
    public GameObject trailRenderer;

    private float distanceSqr;
    private float physicsDistanceSqr;
    private Object[] objects;
    private static List<Object> gameObjects;
    private static List<Object> physicsObjects;
    private static ParticleSystem trailSystem;
    private ParticleSystem.Particle[] particles;
    private ParticleSystem.EmissionModule emissionModule;

    [HideInInspector] public Vector3 absolutePosition;
    [HideInInspector] public Vector3 worldOffset;
    [HideInInspector] public bool originChanged = false;


    void Start()
    {
        distanceSqr = Mathf.Pow(distance, 2f);
        physicsDistanceSqr = Mathf.Pow(physicsDistance, 2f);

        if (trailRenderer != null)
        {
            trailSystem = trailRenderer.GetComponent<ParticleSystem>();

            if (particles == null || particles.Length < trailSystem.main.maxParticles)
                particles = new ParticleSystem.Particle[trailSystem.main.maxParticles];

            emissionModule = trailSystem.emission;
        }

        absolutePosition = transform.position;
    }

    void LateUpdate()
    {
        ManageFloatingOrigin();
    }

    public void CollectObjects()
    {
        gameObjects = new List<Object>();
        gameObjects = FindObjectsOfType(typeof(Transform)).ToList();

        if (checkPhysics)
        {
            physicsObjects = new List<Object>();
            physicsObjects = FindObjectsOfType(typeof(Rigidbody)).ToList();
        }
    }

    private void ManageFloatingOrigin()
    {
        originChanged = false;
        Vector3 cameraPosition = transform.position;
        absolutePosition = transform.position + worldOffset;

        if (cameraPosition.sqrMagnitude > distanceSqr)
        {
            worldOffset += transform.position;
            originChanged = true;

            CollectObjects();
            foreach (Object o in gameObjects)
            {
                try
                {
                    if (o.name == "Building") continue;
                    Transform t = (Transform)o;

                    if (t.parent == null)
                    {
                        t.position -= cameraPosition;
                    }
                }
                catch (MissingReferenceException e)
                {
                    Trace.Warning(
                        "ManageFloatingOrigin(): object was destroyed before accessing its transform: {0}",
                        e.Message);
                }
            }

            if (checkParticles && trailRenderer != null)
            {
                //emissionModule.enabled = false;

                int liveParticles = trailSystem.GetParticles(particles);

                for (int i = 0; i < liveParticles; i++)
                    particles[i].position -= cameraPosition;

                trailSystem.SetParticles(particles, liveParticles);

                //emissionModule.enabled = true;
            }

            if (checkPhysics && physicsDistance > 0f)
            {
                foreach (Object o in physicsObjects)
                {
                    Rigidbody r = (Rigidbody)o;

                    if (r.gameObject.transform.position.sqrMagnitude > physicsDistanceSqr)
                        r.sleepThreshold = float.MaxValue;
                    else
                        r.sleepThreshold = defaultSleepThreshold;
                }
            }
        }
    }
}

