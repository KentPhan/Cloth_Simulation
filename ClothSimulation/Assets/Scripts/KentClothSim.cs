using System.Collections.Generic;
using UnityEngine;

namespace Assets
{

    public class ClothVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Velocity;
        public Vector3 Force;
        public bool Fixed = false;
        public List<ClothVertex> StructuralNeighbors = new List<ClothVertex>();// 1 away, Up Down Left Right
        public List<ClothVertex> ShearNeighbors = new List<ClothVertex>();// Diagnols
        public List<ClothVertex> FlexionNeighbors = new List<ClothVertex>();// 2 away, Up Down Left Right
    }


    public class KentClothSim : MonoBehaviour
    {
        [Header("Force Properties")]
        [SerializeField] private Vector3 GRAVITY_CONSTANT = new Vector3(0.0f, -9.8f, 0.0f);

        

        [Header("Cloth Properties")]
        [SerializeField] [Range(0, 20)] private float VERTEX_MASS = 1.0f;
        
        
        [Header("Spring Properties")]
        [SerializeField] [Range(0, 1000)] private float SPRING_STIFFNESS_CONSTANT_STRUCTURAL = 1.0f;
        [SerializeField] [Range(0, 1000)] private float SPRING_STIFFNESS_CONSTANT_SHEAR = 1.0f;
        [SerializeField] [Range(0, 1000)] private float SPRING_STIFFNESS_CONSTANT_FLEXION = 1.0f;
        [SerializeField] [Range(0, 20)] private float StructuralDistanceBetweenVertexes = 1.0f;

        [Header("Viscous/Wind Properties")]
        [SerializeField] [Range(0, 100)] private float  VISCOUS_CONSTANT= 0.2f;
        [SerializeField]  private Vector3 VISCOUS_FORCE_CONSTANT = new Vector3(1.0f, 0.0f, 0.0f);


        [Header("Dampening Properties")]
        [SerializeField] [Range(0, 1000)] private float FORCE_DAMPENING = 0.5f;
        [SerializeField] [Range(0, 1000)] private float SPRING_DAMPENING = 0.5f;
        [SerializeField] [Range(0, 2)] private float VELOCITY_DAMPENING = 0.5f;
        [SerializeField] [Range(0, 2)] private float GRAVITY_DAMPENING = 0.2f;

        private Dictionary<Vector2Int, ClothVertex> clothVertexStructure;
        private Vector2Int clothResolution = new Vector2Int(11,11);// Hardcoded due to used plane mesh


        // Cached values
        

        private void Awake()
        {
            
        }

        // Start is called before the first frame update
        void Start()
        {
            clothVertexStructure = ConstructClothMeshDataStructure(GetComponent<MeshFilter>().mesh, clothResolution);

            
            
        }


        private static Dictionary<Vector2Int, ClothVertex> ConstructClothMeshDataStructure(Mesh givenMesh,Vector2Int resolution)
        {
            Vector3[] vertices = givenMesh.vertices;
            Vector3[] normals = givenMesh.normals;

            // Construct Dictionary
            Dictionary<Vector2Int, ClothVertex> toReturn = new Dictionary<Vector2Int, ClothVertex>();
            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    int index = (i * resolution.x) + j;
                    Vector3 vertexLocalSpacePosition = vertices[index];
                    Vector3 vertexNormal = normals[index];

                    ClothVertex xVert = new ClothVertex()
                    {
                        Position = vertexLocalSpacePosition,
                        Normal = vertexNormal,
                        Force = Vector3.zero,
                        Velocity = Vector3.zero,
                    };
                    toReturn.Add(new Vector2Int(i, j), xVert);
                }
            }

            // Set Anchor Points
            toReturn[new Vector2Int(0, 0)].Fixed = true;
            toReturn[new Vector2Int(0, 10)].Fixed = true;
            //toReturn[new Vector2Int(10, 10)].Fixed = true;
            //toReturn[new Vector2Int(10, 0)].Fixed = true;


            // Construct Neighbor Lists
            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    Vector2Int key =  new Vector2Int(i, j);

                    GetMassSpringNeighbors(toReturn, key, 
                        out toReturn[key].StructuralNeighbors,
                        out toReturn[key].ShearNeighbors, 
                        out toReturn[key].FlexionNeighbors);
                }
            }

            return toReturn;
        }

        public static void UpdateClothMeshDataNormalsFromDataStructure(
            Dictionary<Vector2Int, ClothVertex> clothStructure, Mesh givenMesh, Vector2Int resolution)
        {
            Vector3[] normals = givenMesh.normals;

            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    int index = (i * resolution.x) + j;
                    Vector3 vertexNormal = normals[index];

                    clothStructure[new Vector2Int(i, j)].Normal = vertexNormal;
                }
            }
        }

        private static void GetMassSpringNeighbors(Dictionary<Vector2Int, ClothVertex> clothStructure, Vector2Int targetKey, out List<ClothVertex> o_structuralNeighbors, out List<ClothVertex> o_shearNeighbors, out List<ClothVertex> o_flexionNeighbors)
        {
            o_structuralNeighbors = new List<ClothVertex>();
            o_shearNeighbors = new List<ClothVertex>();
            o_flexionNeighbors = new List<ClothVertex>();

            // Loop through neighbors and get shear and structural
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Vector2Int offset = new Vector2Int(i, j);
                    if (clothStructure.ContainsKey(targetKey + offset))
                    {

                        if (Mathf.Abs(i) != Mathf.Abs(j))
                        {
                            o_structuralNeighbors.Add(clothStructure[targetKey + offset]);
                        }
                        else
                        {
                            o_shearNeighbors.Add(clothStructure[targetKey + offset]);
                        }
                        
                    }
                }
            }

            // Get Flexion
            {
                Vector2Int offset;
                offset = new Vector2Int(0, 2);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
                offset = new Vector2Int(0, -2);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
                offset = new Vector2Int(2, 0);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
                offset = new Vector2Int(-2, 0);
                if (clothStructure.ContainsKey(targetKey + offset))
                    o_flexionNeighbors.Add(clothStructure[targetKey + offset]);
            }

            return;
        }


        private static Vector3[] ConstructVertexArrayFromDataStructure(Dictionary<Vector2Int, ClothVertex> clothStructure, Vector2Int resolution)
        {
            Vector3[] toReturn = new Vector3[clothStructure.Count];

            for (int i = 0; i < resolution.x; i++)
            {
                for (int j = 0; j < resolution.y; j++)
                {
                    Vector2Int key = new Vector2Int(i, j);
                    toReturn[(i * resolution.x) + j] = clothStructure[key].Position;
                }
            }


            return toReturn;
        }

        private static Vector3 CalculateAdditiveSpringForce(Vector3 positionCenter, Vector3 positionNeighbor, Vector3 velocityCenter, Vector3 velocityNeighbor, float springStiffnessConstant, float springRestLength, float springDampeningConstant)
        {
            Vector3 positionVectorToCenter = (positionCenter - positionNeighbor);
            Vector3 velocityVectorToCenter = (velocityCenter - velocityNeighbor);

            Vector3 springForceDirectionToCenter = positionVectorToCenter.normalized;
            float currentLength = positionVectorToCenter.magnitude;

            // Constant is set to always be positive
            // Current Length Shorter Than Rest = Positive Spring Force Direction To Center = Force Should Go Toward Current Cloth Vertex (Center)
            // Current Length Longer Than Rest = Negative Spring Force Direction To Center = Force Should Go Toward Neighbor
            float springForce = springStiffnessConstant * (springRestLength - currentLength);


            //float dampening = springDampeningConstant *
            //                  (Vector3.Dot(velocityVectorToCenter, positionVectorToCenter));

            //float dampening = springStiffnessConstant * Vector3.Project(velocityCenter, springForceDirectionToCenter).magnitude;

            // Projected Velocities In Same Direction As Spring Force
            float dampening = -springDampeningConstant * (Vector3.Dot(velocityCenter, springForceDirectionToCenter) - Vector3.Dot(velocityNeighbor, springForceDirectionToCenter)); // Project Velocities along spring direction
            //float dampening = -springDampeningConstant * (Vector3.Dot(velocityCenter, springForceDirectionToCenter)); // Project Velocities along spring direction


            //float dampening = (-springDampeningConstant * springForce);


            //force = Mathf.Max(force - (this.SPRING_DAMPENING * force),0.0f);
            //Debug.Log(springForce);
            //if (springForce < 0.01f)
            //    springForce = 0.0f;
            return springForceDirectionToCenter * (springForce + dampening);
        }

        

        // Update is called once per frame
        void Update()
        {
          

        }


        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            //Debug.Log(deltaTime);
            UpdateSimulateCloth(this.clothVertexStructure, deltaTime);

            Mesh givenMesh = GetComponent<MeshFilter>().mesh;
            givenMesh.vertices = ConstructVertexArrayFromDataStructure(this.clothVertexStructure, this.clothResolution);
            givenMesh.RecalculateNormals();
            givenMesh.RecalculateTangents();
            UpdateClothMeshDataNormalsFromDataStructure(this.clothVertexStructure, givenMesh, this.clothResolution);
        }




        private void UpdateSimulateCloth(Dictionary<Vector2Int, ClothVertex> clothStructure, float deltaTime)
        {

            

            foreach (KeyValuePair<Vector2Int, ClothVertex> keyPair in clothStructure)
            {

                Vector2Int key = keyPair.Key;
                ClothVertex currentClothVertex = keyPair.Value;
                Vector3 currentVelocity = currentClothVertex.Velocity;
                Vector3 currentNormal = currentClothVertex.Normal;


                


                // Calculate Forces
                {
                    ////Gravity
                    Vector3 gravityForce = (GRAVITY_CONSTANT * VERTEX_MASS);
                    currentClothVertex.Force += gravityForce;


                    // TODO Wind

                    // TODO Air Resistance


                    //Spring Forces
                    Vector3 springForce = Vector3.zero;
                    float restLengthStructural = StructuralDistanceBetweenVertexes;// Equivalent to With of Cloth/ Vertexes in a a width
                    float restLengthShear = restLengthStructural * Mathf.Sqrt(2.0f);
                    float restLengthFlexion = restLengthStructural * 2.0f;
                    foreach (ClothVertex neighbor in currentClothVertex.StructuralNeighbors)
                    {
                        springForce += CalculateAdditiveSpringForce(currentClothVertex.Position, neighbor.Position, currentVelocity, neighbor.Velocity,
                            this.SPRING_STIFFNESS_CONSTANT_STRUCTURAL, restLengthStructural, SPRING_DAMPENING);
                    }
                    foreach (ClothVertex neighbor in currentClothVertex.ShearNeighbors)
                    {
                        springForce += CalculateAdditiveSpringForce(currentClothVertex.Position, neighbor.Position, currentVelocity, neighbor.Velocity,
                            this.SPRING_STIFFNESS_CONSTANT_SHEAR, restLengthShear, SPRING_DAMPENING);
                    }
                    foreach (ClothVertex neighbor in currentClothVertex.FlexionNeighbors)
                    {
                        springForce += CalculateAdditiveSpringForce(currentClothVertex.Position, neighbor.Position, currentVelocity, neighbor.Velocity,
                            this.SPRING_STIFFNESS_CONSTANT_FLEXION, restLengthFlexion, SPRING_DAMPENING);
                    }


                    // Add in spring Force
                    currentClothVertex.Force += springForce;

                    // Dampening All Forces
                    //Debug.Log(currentClothVertex.Force);
                    currentClothVertex.Force = currentClothVertex.Force - (currentClothVertex.Force * FORCE_DAMPENING);

                    //Vector3 dampening = -FORCE_DAMPENING * currentVelocity;
                    //currentClothVertex.Force += (dampening);

                    //aDebug.Log(currentClothVertex.Force);

                    //if (Vector3.Dot(dampening, currentClothVertex.Force) > 0)
                    //{
                    //    Debug.Log("SAME DIRECTION");
                    //}
                    //else 
                    //{
                    //    Debug.Log("DIFFERENT DIRECTION");
                    //}


                    //if (dampening.magnitude > currentClothVertex.Force.magnitude)
                    //{
                    //    Debug.Log("GREATER");
                    //}
                    //else
                    //{
                    //    Debug.Log("LESSER");
                    //}

                    // Viscous/Wind Fluid Force
                    //currentClothVertex.Force += (VISCOUS_CONSTANT * (Vector3.Dot(currentNormal, VISCOUS_FORCE_CONSTANT - currentVelocity)) * currentNormal);


                    // Air Resistance
                    //currentClothVertex.Force +=
                    //    ((-currentClothVertex.Force / VERTEX_MASS) * Mathf.Pow(currentVelocity, 2.0f));
                }

                
                // Integrate Velocity
                {
                    currentClothVertex.Velocity = currentClothVertex.Velocity +
                                                  ((currentClothVertex.Force / VERTEX_MASS) * (deltaTime));

                    //// Dampening Velocity
                    currentClothVertex.Velocity =
                        currentClothVertex.Velocity - (currentClothVertex.Velocity * VELOCITY_DAMPENING);

                }

                // Integrate Position
                {
                    if (!currentClothVertex.Fixed)
                    {
                        currentClothVertex.Position = currentClothVertex.Position +
                                                      ((currentClothVertex.Velocity) * (deltaTime));
                    }
                    
                }

                //if (keyPair.Key.x % 2 == 0)
                //{
                //    //keyPair.Value.Position.x = Mathf.Sin(Time.time);
                //    //vertices[i] += normals[i] * Mathf.Sin(Time.time);
                //}
            }
        }

        private void OnDrawGizmos()
        {

            if (this.clothVertexStructure != null)
            {
                Matrix4x4 transform = this.transform.localToWorldMatrix;

                foreach (KeyValuePair<Vector2Int, ClothVertex> keyPair in this.clothVertexStructure)
                {
                    ClothVertex vert = keyPair.Value;

                    Vector3 start = transform.MultiplyPoint(vert.Position);
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(start, 0.1f);


                    // Draw Force Vector
                    {
                        Gizmos.color = Color.red;
                        Vector3 end = start + vert.Force.normalized * 1.0f;
                        Gizmos.DrawLine(start, end);
                    }


                    // Draw Normal
                    {
                        Gizmos.color = Color.green;
                        Vector3 end = start + vert.Normal.normalized * 1.0f;
                        Gizmos.DrawLine(start, end);
                    }

                }



                Vector2Int key = new  Vector2Int(2,2);
                List<ClothVertex> structuralNeighbors;
                List<ClothVertex> shearNeighbors;
                List<ClothVertex> flexiconNeighbors;
                GetMassSpringNeighbors(this.clothVertexStructure, key, out structuralNeighbors, out shearNeighbors, out flexiconNeighbors);

                // Color Center
                {
                    var centerVertex = this.clothVertexStructure[key];
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(transform.MultiplyPoint(centerVertex.Position), 0.1f);
                }


                // Color neighbors
                {
                    Gizmos.color = Color.red;
                    foreach (var neighbor in structuralNeighbors)
                    {
                        Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                    }

                    Gizmos.color = Color.green;
                    foreach (var neighbor in shearNeighbors)
                    {
                        Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                    }

                    Gizmos.color = Color.blue;
                    foreach (var neighbor in flexiconNeighbors)
                    {
                        Gizmos.DrawSphere(transform.MultiplyPoint(neighbor.Position), 0.1f);
                    }
                }
                
            }

            else
            {
                Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.vertices.Length; i++)
                {
                    Vector3 vertex = mesh.vertices[i];

                    vertex = this.transform.localToWorldMatrix.MultiplyPoint(vertex);

                    Gizmos.DrawSphere(vertex, 0.1f);
                }
            }
        }
    }
}
