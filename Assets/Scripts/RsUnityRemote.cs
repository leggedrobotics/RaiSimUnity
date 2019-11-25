/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace raisimUnity
{
    enum ClientStatus : int
    {
        Idle = 0,
        InitializeObjectsStart,      // start 
        InitializingObjects,
        InitializeVisualsStart,      // start
        InitializingVisuals,
        UpdateScene,
        ReinitializeObjectsStart,    // start  
        ReinitializingObjects,
    }
    
    enum RsObejctType : int
    {
        RsSphereObject = 0, 
        RsBoxObject,
        RsCylinderObject,
        RsConeObject, 
        RsCapsuleObject,
        RsMeshObject,
        RsHalfSpaceObject, 
        RsCompoundObject,
        RsHeightMapObject,
        RsArticulatedSystemObject,
    }

    enum RsShapeType : int
    {
        RsBoxShape = 0, 
        RsCylinderShape,
        RsSphereShape,
        RsMeshShape,
        RsCapsuleShape, 
        RsConeShape,
    }

    enum RsVisualType : int
    {
        RsVisualSphere = 0,
        RsVisualBox,
        RsVisualCylinder,
        RsVisualCapsule,
        RsVisualMesh,
    }

    static class VisualTag
    {
        public const string Visual = "visual";
        public const string Collision = "collision";
        public const string Frame = "frame";
    }

    public class RsUnityRemote : MonoBehaviour
    {
        // Prevent repeated instances
        private static RsUnityRemote instance;
        
        private XmlReader _xmlReader;
        private ResourceLoader _loader;
        private TcpHelper _tcpHelper;
        
        private RsUnityRemote()
        {
            _tcpHelper = new TcpHelper();
            _xmlReader = new XmlReader();
            _loader = new ResourceLoader();
        }
        
        public static RsUnityRemote Instance
        {
            get 
            {
                if( instance==null )
                {
                    instance = new RsUnityRemote();
                    return instance;
                }
                else
                    throw new System.Exception("TCPRemote can only be instantiated once");
            }
        }
        
        // Status
        private ClientStatus _clientStatus;

        // Visualization
        private bool _showVisualBody = true;
        private bool _showCollisionBody = false;
        private bool _showContactPoints = false;
        private bool _showContactForces = false;
        private bool _showBodyFrames = false;
        private float _contactPointMarkerScale = 1;
        private float _contactForceMarkerScale = 1;
        private float _bodyFrameMarkerScale = 1;
        
        // Root objects
        private GameObject _objectsRoot;
        private GameObject _visualsRoot;
        private GameObject _contactPointsRoot;
        private GameObject _contactForcesRoot;
        private GameObject _objectCache;
        
        // Object controller 
        private ObjectController _objectController;
        private ulong _numInitializedObjects;
        private ulong _numWorldObjects; 
        private ulong _numInitializedVisuals;
        private ulong _numWorldVisuals;
        
        // Shaders
        private Shader _transparentShader;
        private Shader _standardShader;
        
        // Default materials
        private Material _planeMaterial;
        private Material _terrainMaterial;
        private Material _defaultMaterialR;
        private Material _defaultMaterialG;
        private Material _defaultMaterialB;

        // Modal view
        private ErrorViewController _errorModalView;
        private LoadingViewController _loadingModalView;
        
        // Configuration number (should be always matched with server)
        private ulong _configurationNumber = 0; 
        
        void Awake()
        {
            // object roots
            _objectsRoot = new GameObject("_RsObjects");
            _objectsRoot.transform.SetParent(transform);
            _objectCache = new GameObject("_ObjectCache");
            _objectCache.transform.SetParent(transform);
            _visualsRoot = new GameObject("_VisualObjects");
            _visualsRoot.transform.SetParent(transform);
            _contactPointsRoot = new GameObject("_ContactPoints");
            _contactPointsRoot.transform.SetParent(transform);
            _contactForcesRoot = new GameObject("_ContactForces");
            _contactForcesRoot.transform.SetParent(transform);

            // object controller 
            _objectController = new ObjectController(_objectCache);

            // shaders
            _standardShader = Shader.Find("Standard");
            _transparentShader = Shader.Find("RaiSim/Transparent");
            
            // materials
            _planeMaterial = Resources.Load<Material>("Tiles1");
            _terrainMaterial = Resources.Load<Material>("Ground1");
            _defaultMaterialR = Resources.Load<Material>("Plastic1");
            _defaultMaterialG = Resources.Load<Material>("Plastic2");
            _defaultMaterialB = Resources.Load<Material>("Plastic3");
            
            // ui controller 
            _errorModalView = GameObject.Find("_CanvasModalViewError").GetComponent<ErrorViewController>();
            _loadingModalView = GameObject.Find("_CanvasModalViewLoading").GetComponent<LoadingViewController>();
        }

        void Start()
        {
            _clientStatus = ClientStatus.Idle;
        }

        public void EstablishConnection()
        {
            _tcpHelper.EstablishConnection();
            _clientStatus = ClientStatus.InitializeObjectsStart;
        }

        public void CloseConnection()
        {
            ClearScene();
            
            _tcpHelper.CloseConnection();
            _clientStatus = ClientStatus.Idle;
        }

        void Update()
        {
            // broken connection: clear
            if( !_tcpHelper.CheckConnection() )
            {
                CloseConnection();
            }

            // data available: handle communication
            if (_tcpHelper.DataAvailable)
            {
                try
                {
                    switch (_clientStatus)
                    {
                        //**********************************************************************************************
                        // Step 0
                        //**********************************************************************************************
                        case ClientStatus.Idle:
                        {
                            break;
                        }
                        //**********************************************************************************************
                        // Step 1
                        //**********************************************************************************************
                        case ClientStatus.InitializeObjectsStart:
                        {
                            _loadingModalView.Show(true);
                            _loadingModalView.SetTitle("Initializing RaiSim Objects");
                            _loadingModalView.SetMessage("Loading resources...");
                            _loadingModalView.SetProgress(0);

                            // Read XML string
                            ReadXmlString();
                            
                            // Start initialization
                            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestInitialization));
                            if (_tcpHelper.ReadData() <= 0)
                                throw new RsuInitSceneException("Cannot read data from TCP");

                            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
                            if (state == ServerStatus.StatusTerminating)
                                throw new RsuInitSceneException("Server is terminating");

                            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
                            if (messageType != ServerMessageType.Initialization)
                                throw new RsuInitSceneException("Server gives wrong message");

                            _configurationNumber = _tcpHelper.GetData<ulong>();
                            _numWorldObjects = _tcpHelper.GetData<ulong>();
                            _numInitializedObjects = 0;
                            _clientStatus = ClientStatus.InitializingObjects;
                            break;
                        }
                        //**********************************************************************************************
                        // Step 2
                        //**********************************************************************************************
                        case ClientStatus.InitializingObjects:
                        {
                            if (_numInitializedObjects < _numWorldObjects)
                            {
                                // Initialize objects from data
                                // If the function call time is > 0.1 sec, rest of objects are initialized in next Update iteration
                                PartiallyInitializeScene();
                                _loadingModalView.SetProgress((float) _numInitializedObjects / _numWorldObjects);
                            }
                            else if (_numInitializedObjects == _numWorldObjects)
                            {
                                // Initialization done 
                                _loadingModalView.Show(false);
                                _clientStatus = ClientStatus.InitializeVisualsStart;
                            }
                            else
                            {
                                // TODO error
                            }

                            break;
                        }
                        //**********************************************************************************************
                        // Step 3
                        //**********************************************************************************************
                        case ClientStatus.InitializeVisualsStart:
                        {
                            _loadingModalView.Show(true);
                            _loadingModalView.SetTitle("Initializing Visuals");
                            _loadingModalView.SetMessage("Loading resources...");
                            _loadingModalView.SetProgress(0);
                            
                            // Start initialization
                            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestInitializeVisuals));
                            if (_tcpHelper.ReadData() <= 0)
                                throw new RsuInitVisualsException("Cannot read data from TCP");

                            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
                            if (state == ServerStatus.StatusTerminating)
                                throw new RsuInitVisualsException("Server is terminating");

                            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
                            if (messageType != ServerMessageType.VisualInitialization)
                                throw new RsuInitVisualsException("Server gives wrong message");

                            _numWorldVisuals = _tcpHelper.GetData<ulong>();
                            _numInitializedVisuals = 0;
                            _clientStatus = ClientStatus.InitializingVisuals;
                            break;
                        }
                        //**********************************************************************************************
                        // Step 4
                        //**********************************************************************************************
                        case ClientStatus.InitializingVisuals:
                        {
                            if (_numInitializedVisuals < _numWorldVisuals)
                            {
                                // Initialize visuals from data
                                // If the function call time is > 0.1 sec, rest of objects are initialized in next Update iteration
                                PartiallyInitializeVisuals();
                                _loadingModalView.SetProgress((float) _numInitializedObjects / _numWorldObjects);
                            }
                            else if (_numInitializedVisuals == _numWorldVisuals)
                            {
                                // Initialization done 
                                _loadingModalView.Show(false);
                                
                                // Disable other cameras than main camera
                                foreach (var cam in Camera.allCameras)
                                {
                                    if (cam == Camera.main) continue;
                                    cam.enabled = false;
                                }

                                // Show / hide objects
                                ShowOrHideObjects();
                                
                                _clientStatus = ClientStatus.UpdateScene;
                            }
                            else
                            {
                                // TODO error
                            }
                            break;
                        }
                        //**********************************************************************************************
                        // Step 5
                        //**********************************************************************************************
                        case ClientStatus.UpdateScene:
                        {
                            // update object position
                            UpdateObjectsPosition();

                            // update visuals
                            UpdateVisualsPosition();
                        
                            // update contacts
                            UpdateContacts();
                            break;
                        }
                        case ClientStatus.ReinitializeObjectsStart:
                        {
                            // If server side has been changed, initialize objects
                            // Clear objects first
                            foreach (Transform objT in _objectsRoot.transform)
                            {
                                Destroy(objT.gameObject);
                            }
                            
                            // Start reinitializing
                            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestInitialization));
                            if (_tcpHelper.ReadData() <= 0)
                                throw new RsuInitSceneException("Cannot read data from TCP");

                            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
                            if (state == ServerStatus.StatusTerminating)
                                throw new RsuInitSceneException("Server is terminating");

                            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
                            if (messageType != ServerMessageType.Initialization)
                                throw new RsuInitSceneException("Server gives wrong message");

                            _configurationNumber = _tcpHelper.GetData<ulong>();
                            _numWorldObjects = _tcpHelper.GetData<ulong>();
                            _numInitializedObjects = 0;
                            _clientStatus = ClientStatus.ReinitializingObjects;
                            break;
                        }
                        case ClientStatus.ReinitializingObjects:
                        {
                            if (_numInitializedObjects < _numWorldObjects)
                            {
                                // Reinitialize objects from data
                                // If the function call time is > 0.1 sec, rest of objects are initialized in next Update iteration
                                PartiallyInitializeScene();
                            }
                            else if (_numInitializedObjects == _numWorldObjects)
                            {
                                // Reinitialization done 
                                _clientStatus = ClientStatus.UpdateScene;
                                
                                // Disable other cameras than main camera
                                foreach (var cam in Camera.allCameras)
                                {
                                    if (cam == Camera.main) continue;
                                    cam.enabled = false;
                                }

                                // Show / hide objects
                                ShowOrHideObjects();
                            }
                            else
                            {
                                // TODO error
                            }

                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    // modal view
                    _errorModalView.Show(true);
                    _errorModalView.SetMessage(e.Message);

                    // close connection
                    _tcpHelper.CloseConnection();
                }
            }
        }

        private void ClearScene()
        {
            // objects
            foreach (Transform objT in _objectsRoot.transform)
            {
                Destroy(objT.gameObject);
            }
            
            // contact points
            foreach (Transform objT in _contactPointsRoot.transform)
            {
                Destroy(objT.gameObject);
            }
            
            // contact forces
            foreach (Transform child in _contactForcesRoot.transform)
            {
                Destroy(child.gameObject);
            }
            
            // visuals
            foreach (Transform child in _visualsRoot.transform)
            {
                Destroy(child.gameObject);
            }
            
            // clear appearances
            if(_xmlReader != null)
                _xmlReader.ClearAppearanceMap();
            
            // clear modal view
            _loadingModalView.Show(false);
            
            // clear object cache
            _objectController.ClearCache();
        }

        private void ClearContacts()
        {
            // contact points
            foreach (Transform objT in _contactPointsRoot.transform)
            {
                Destroy(objT.gameObject);
            }
            
            // contact forces
            foreach (Transform child in _contactForcesRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void PartiallyInitializeScene()
        {
            while (_numInitializedObjects < _numWorldObjects)
            {
                ulong objectIndex = _tcpHelper.GetData<ulong>();
                RsObejctType objectType = _tcpHelper.GetData<RsObejctType>();
                
                // get name and find corresponding appearance from XML
                string name = _tcpHelper.GetData<string>();
                Appearances? appearances = _xmlReader.FindApperancesFromObjectName(name);
                
                if (objectType == RsObejctType.RsArticulatedSystemObject)
                {
                    string urdfDirPathInServer = _tcpHelper.GetData<string>(); 

                    // visItem = 0 (visuals)
                    // visItem = 1 (collisions)
                    for (int visItem = 0; visItem < 2; visItem++)
                    {
                        ulong numberOfVisObjects = _tcpHelper.GetData<ulong>();

                        for (ulong j = 0; j < numberOfVisObjects; j++)
                        {
                            RsShapeType shapeType = _tcpHelper.GetData<RsShapeType>();
                                
                            ulong group = _tcpHelper.GetData<ulong>();

                            string subName = Path.Combine(objectIndex.ToString(), visItem.ToString(), j.ToString());
                            var objFrame = _objectController.CreateRootObject(_objectsRoot, subName);

                            string tag = "";
                            if (visItem == 0)
                                tag = VisualTag.Visual;
                            else if (visItem == 1)
                                tag = VisualTag.Collision;

                            if (shapeType == RsShapeType.RsMeshShape)
                            {
                                string meshFile = _tcpHelper.GetData<string>();
                                string meshFileExtension = Path.GetExtension(meshFile);

                                double sx = _tcpHelper.GetData<double>();
                                double sy = _tcpHelper.GetData<double>();
                                double sz = _tcpHelper.GetData<double>();

                                string meshFilePathInResourceDir = _loader.RetrieveMeshPath(urdfDirPathInServer, meshFile);
                                if (meshFilePathInResourceDir == null)
                                {
                                    throw new RsuInitSceneException("Cannot find mesh from resource directories = " + meshFile);
                                }

                                try
                                {
                                    var mesh = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, (float)sx, (float)sy, (float)sz);
                                    mesh.tag = tag;
                                }
                                catch (Exception e)
                                {
                                    throw new RsuInitSceneException("Cannot create mesh: " + e.Message);
                                    throw;
                                }
                            }
                            else
                            {
                                ulong size = _tcpHelper.GetData<ulong>();
                                    
                                var visParam = new List<double>();
                                for (ulong k = 0; k < size; k++)
                                {
                                    double visSize = _tcpHelper.GetData<double>();
                                    visParam.Add(visSize);
                                }
                                switch (shapeType)
                                {
                                    case RsShapeType.RsBoxShape:
                                    {
                                        if (visParam.Count != 3) throw new RsuInitSceneException("Box Mesh error");
                                        var box = _objectController.CreateBox(objFrame, (float) visParam[0], (float) visParam[1], (float) visParam[2]);
                                        box.tag = tag;
                                    }
                                        break;
                                    case RsShapeType.RsCapsuleShape:
                                    {
                                        if (visParam.Count != 2) throw new RsuInitSceneException("Capsule Mesh error");
                                        var capsule = _objectController.CreateCapsule(objFrame, (float)visParam[0], (float)visParam[1]);
                                        capsule.tag = tag;
                                    }
                                        break;
                                    case RsShapeType.RsConeShape:
                                    {
                                        // TODO URDF does not support cone shape
                                    }
                                        break;
                                    case RsShapeType.RsCylinderShape:
                                    {
                                        if (visParam.Count != 2) throw new RsuInitSceneException("Cylinder Mesh error");
                                        var cylinder = _objectController.CreateCylinder(objFrame, (float)visParam[0], (float)visParam[1]);
                                        cylinder.tag = tag;
                                    }
                                        break;
                                    case RsShapeType.RsSphereShape:
                                    {
                                        if (visParam.Count != 1) throw new RsuInitSceneException("Sphere Mesh error");
                                        var sphere = _objectController.CreateSphere(objFrame, (float)visParam[0]);
                                        sphere.tag = tag;
                                    }
                                        break;
                                }
                            }
                        }
                    }
                }
                else if (objectType == RsObejctType.RsHalfSpaceObject)
                {
                    // get material
                    Material material;
                    if (appearances != null && !string.IsNullOrEmpty(appearances.As<Appearances>().materialName))
                    {
                        material = Resources.Load<Material>(appearances.As<Appearances>().materialName);
                    }
                    else
                    {
                        // default material
                        material = _planeMaterial;
                    }
                    
                    float height = _tcpHelper.GetData<float>();
                    var objFrame = _objectController.CreateRootObject(_objectsRoot, objectIndex.ToString());
                    var plane = _objectController.CreateHalfSpace(objFrame, height);
                    plane.tag = VisualTag.Collision;

                    // default visual object
                    if (appearances == null || !appearances.As<Appearances>().subAppearances.Any())
                    {
                        var planeVis = _objectController.CreateHalfSpace(objFrame, height);
                        planeVis.GetComponentInChildren<Renderer>().material = material;
                        planeVis.GetComponentInChildren<Renderer>().material.mainTextureScale = new Vector2(5, 5);
                        planeVis.tag = VisualTag.Visual;
                    }
                }
                else if (objectType == RsObejctType.RsHeightMapObject)
                {
                    // get material
                    Material material;
                    if (appearances != null && !string.IsNullOrEmpty(appearances.As<Appearances>().materialName))
                    {
                        material = Resources.Load<Material>(appearances.As<Appearances>().materialName);
                    }
                    else
                    {
                        // default material
                        material = _terrainMaterial;
                    }
                    
                    // center
                    float centerX = _tcpHelper.GetData<float>();
                    float centerY = _tcpHelper.GetData<float>();
                    // size
                    float sizeX = _tcpHelper.GetData<float>();
                    float sizeY = _tcpHelper.GetData<float>();
                    // num samples
                    ulong numSampleX = _tcpHelper.GetData<ulong>();
                    ulong numSampleY = _tcpHelper.GetData<ulong>();
                    ulong numSample = _tcpHelper.GetData<ulong>();
                        
                    // height values 
                    float[,] heights = new float[numSampleY, numSampleX];
                    for (ulong j = 0; j < numSampleY; j++)
                    {
                        for (ulong k = 0; k < numSampleX; k++)
                        {
                            float height = _tcpHelper.GetData<float>();
                            heights[j, k] = height;
                        }
                    }

                    var objFrame = _objectController.CreateRootObject(_objectsRoot, objectIndex.ToString());
                    var terrain = _objectController.CreateTerrain(objFrame, numSampleX, sizeX, centerX, numSampleY, sizeY, centerY, heights, false);
                    terrain.tag = VisualTag.Collision;
                    
                    // default visual object
                    if (appearances == null || !appearances.As<Appearances>().subAppearances.Any())
                    {
                        var terrainVis = _objectController.CreateTerrain(objFrame, numSampleX, sizeX, centerX, numSampleY, sizeY, centerY, heights);
                        terrainVis.GetComponentInChildren<Renderer>().material = material;
                        terrainVis.GetComponentInChildren<Renderer>().material.mainTextureScale = new Vector2(sizeX, sizeY);
                        terrainVis.tag = VisualTag.Visual;
                    }
                }
                else
                {
                    // single body object
                    
                    // create base frame of object
                    var objFrame = _objectController.CreateRootObject(_objectsRoot, objectIndex.ToString());
                    
                    // get material
                    Material material;
                    if (appearances != null && !string.IsNullOrEmpty(appearances.As<Appearances>().materialName))
                        material = Resources.Load<Material>(appearances.As<Appearances>().materialName);
                    else
                    {
                        // default material
                        switch (_numInitializedObjects % 3)
                        {
                            case 0:
                                material = _defaultMaterialR;
                                break;
                            case 1:
                                material = _defaultMaterialG;
                                break;
                            case 2:
                                material = _defaultMaterialB;
                                break;
                            default:
                                material = _defaultMaterialR;
                                break;
                        }
                    }
                    
                    // collision body 
                    GameObject collisionObject = null;
                    
                    switch (objectType) 
                    {
                        case RsObejctType.RsSphereObject :
                        {
                            float radius = _tcpHelper.GetData<float>();
                            collisionObject =  _objectController.CreateSphere(objFrame, radius);
                            collisionObject.tag = VisualTag.Collision;
                        }
                            break;

                        case RsObejctType.RsBoxObject :
                        {
                            float sx = _tcpHelper.GetData<float>();
                            float sy = _tcpHelper.GetData<float>();
                            float sz = _tcpHelper.GetData<float>();
                            collisionObject = _objectController.CreateBox(objFrame, sx, sy, sz);
                            collisionObject.tag = VisualTag.Collision;
                        }
                            break;
                        case RsObejctType.RsCylinderObject:
                        {
                            float radius = _tcpHelper.GetData<float>();
                            float height = _tcpHelper.GetData<float>();
                            collisionObject = _objectController.CreateCylinder(objFrame, radius, height);
                            collisionObject.tag = VisualTag.Collision;
                        }
                            break;
                        case RsObejctType.RsCapsuleObject:
                        {
                            float radius = _tcpHelper.GetData<float>();
                            float height = _tcpHelper.GetData<float>();
                            collisionObject = _objectController.CreateCapsule(objFrame, radius, height);
                            collisionObject.tag = VisualTag.Collision;
                        }
                            break;
                        case RsObejctType.RsMeshObject:
                        {
                            string meshFile = _tcpHelper.GetData<string>();
                            float scale = _tcpHelper.GetData<float>();
                            
                            string meshFileName = Path.GetFileName(meshFile);       
                            string meshFileExtension = Path.GetExtension(meshFile);
                            
                            string meshFilePathInResourceDir = _loader.RetrieveMeshPath(Path.GetDirectoryName(meshFile), meshFileName);
                            
                            collisionObject = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, 
                                scale, scale, scale);
                            collisionObject.tag = VisualTag.Collision;
                        }
                            break;
                    }
                    
                    // visual body
                    GameObject visualObject = null;

                    if (appearances != null)
                    {
                        foreach (var subapp in appearances.As<Appearances>().subAppearances)
                        {
                            // subapp material 
                            if(!String.IsNullOrEmpty(subapp.materialName))
                                material = Resources.Load<Material>(subapp.materialName);
                            
                            switch (subapp.shapes)
                            {
                                case AppearanceShapes.Sphere:
                                {
                                    float radius = subapp.dimension.x;
                                    visualObject =  _objectController.CreateSphere(objFrame, radius);
                                    visualObject.GetComponentInChildren<Renderer>().material = material;
                                    visualObject.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Box:
                                {
                                    visualObject = _objectController.CreateBox(objFrame, subapp.dimension.x, subapp.dimension.y, subapp.dimension.z);
                                    visualObject.GetComponentInChildren<Renderer>().material = material;
                                    visualObject.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Cylinder:
                                {
                                    visualObject = _objectController.CreateCylinder(objFrame, subapp.dimension.x, subapp.dimension.y);
                                    visualObject.GetComponentInChildren<Renderer>().material = material;
                                    visualObject.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Capsule:
                                {
                                    visualObject = _objectController.CreateCapsule(objFrame, subapp.dimension.x, subapp.dimension.y);
                                    visualObject.GetComponentInChildren<Renderer>().material = material;
                                    visualObject.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Mesh:
                                {
                                    string meshFileName = Path.GetFileName(subapp.fileName);       
                                    string meshFileExtension = Path.GetExtension(subapp.fileName);
                                    string meshFilePathInResourceDir = _loader.RetrieveMeshPath(Path.GetDirectoryName(subapp.fileName), meshFileName);
                            
                                    visualObject = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, 
                                        subapp.dimension.x, subapp.dimension.y, subapp.dimension.z);
                                    visualObject.GetComponentInChildren<Renderer>().material = material;
                                    visualObject.tag = VisualTag.Visual;
                                }
                                    break;
                                default:
                                    throw new RsuInitSceneException("Not Implemented Appearance Shape");
                            }
                        }
                    }
                    else
                    {
                        // default visual object (same shape with collision)
                        visualObject = GameObject.Instantiate(collisionObject, objFrame.transform);
                        visualObject.GetComponentInChildren<Renderer>().material = material;
                        visualObject.tag = VisualTag.Visual;
                    }
                }

                _numInitializedObjects++;
                if (Time.deltaTime > 0.03f)
                    // If initialization takes too much time, do the rest in next iteration (to prevent freezing GUI(
                    break;
            }
        }

        private void PartiallyInitializeVisuals()
        {
            while (_numInitializedVisuals < _numWorldVisuals)
            {
                RsVisualType objectType = _tcpHelper.GetData<RsVisualType>();
                
                // get name and find corresponding appearance from XML
                string objectName = _tcpHelper.GetData<string>();
                
                float colorR = _tcpHelper.GetData<float>();
                float colorG = _tcpHelper.GetData<float>();
                float colorB = _tcpHelper.GetData<float>();
                float colorA = _tcpHelper.GetData<float>();
                string materialName = _tcpHelper.GetData<string>();
                bool glow = _tcpHelper.GetData<bool>();
                bool shadow = _tcpHelper.GetData<bool>();

                var visFrame = _objectController.CreateRootObject(_visualsRoot, objectName);
                
                GameObject visual = null;
                    
                switch (objectType)
                {
                    case RsVisualType.RsVisualSphere :
                    {
                        float radius = _tcpHelper.GetData<float>();
                        visual =  _objectController.CreateSphere(visFrame, radius);
                        visual.tag = VisualTag.Visual;
                    }
                        break;
                    case RsVisualType.RsVisualBox:
                    {
                        float sx = _tcpHelper.GetData<float>();
                        float sy = _tcpHelper.GetData<float>();
                        float sz = _tcpHelper.GetData<float>();
                        visual = _objectController.CreateBox(visFrame, sx, sy, sz);
                        visual.tag = VisualTag.Visual;
                    }
                        break;
                    case RsVisualType.RsVisualCylinder:
                    {
                        float radius = _tcpHelper.GetData<float>();
                        float height = _tcpHelper.GetData<float>();
                        visual = _objectController.CreateCylinder(visFrame, radius, height);
                        visual.tag = VisualTag.Visual;
                    }
                        break;
                    case RsVisualType.RsVisualCapsule:
                    {
                        float radius = _tcpHelper.GetData<float>();
                        float height = _tcpHelper.GetData<float>();
                        visual = _objectController.CreateCapsule(visFrame, radius, height);
                        visual.tag = VisualTag.Visual;
                    }
                        break;
                }
                
                // set material or color
                if (string.IsNullOrEmpty(materialName) && visual != null)
                {
                    // set material by rgb 
                    visual.GetComponentInChildren<Renderer>().material.color = new Color(colorR, colorG, colorB, colorA);
                    if(glow)
                    {
                        visual.GetComponentInChildren<Renderer>().material.EnableKeyword("_EMISSION");
                        visual.GetComponentInChildren<Renderer>().material.SetColor(
                            "_EmissionColor", new Color(colorR, colorG, colorB, colorA));
                    }
                }
                else
                {
                    // set material from
                    Material material = Resources.Load<Material>(materialName);
                    visual.GetComponentInChildren<Renderer>().material = material;
                }
                
                // set shadow 
                if (shadow)
                {
                    visual.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                }
                else
                {
                    visual.GetComponentInChildren<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }

                _numInitializedVisuals++;
                if (Time.deltaTime > 0.03f)
                    // If initialization takes too much time, do the rest in next iteration (to prevent freezing GUI(
                    break;
            }
        }
        
        private void UpdateObjectsPosition()
        {
            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestObjectPosition));
            if (_tcpHelper.ReadData() <= 0)
                throw new RsuUpdateObjectsPositionException("Cannot read data from TCP");

            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
            if (state == ServerStatus.StatusTerminating)
                throw new RsuUpdateObjectsPositionException("Server is terminating");

            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
            if (messageType != ServerMessageType.ObjectPositionUpdate)
                throw new RsuUpdateObjectsPositionException("Server gives wrong message");
            
            ulong configurationNumber = _tcpHelper.GetData<ulong>();
            if (configurationNumber != _configurationNumber)
            {
                // this means the object was added or deleted from server size
                _clientStatus = ClientStatus.ReinitializeObjectsStart;
                return;
            }

            ulong numObjects = _tcpHelper.GetData<ulong>();

            for (ulong i = 0; i < numObjects; i++)
            {
                ulong localIndexSize = _tcpHelper.GetData<ulong>();

                for (ulong j = 0; j < localIndexSize; j++)
                {
                    string objectName = _tcpHelper.GetData<string>();
                    
                    double posX = _tcpHelper.GetData<double>();
                    double posY = _tcpHelper.GetData<double>();
                    double posZ = _tcpHelper.GetData<double>();
                    
                    double quatW = _tcpHelper.GetData<double>();
                    double quatX = _tcpHelper.GetData<double>();
                    double quatY = _tcpHelper.GetData<double>();
                    double quatZ = _tcpHelper.GetData<double>();

                    GameObject localObject = GameObject.Find(objectName);

                    if (localObject != null)
                    {
                        ObjectController.SetTransform(
                            localObject, 
                            new Vector3((float)posX, (float)posY, (float)posZ), 
                            new Quaternion((float)quatX, (float)quatY, (float)quatZ, (float)quatW)
                        );
                    }
                    else
                    {
                        throw new RsuUpdateObjectsPositionException("Cannot find unity game object: " + objectName);
                    }
                }
            }
        }

        private void UpdateVisualsPosition()
        {
            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestVisualPosition));
            if (_tcpHelper.ReadData() <= 0)
                throw new RsuUpdateVisualsPositionException("Cannot read data from TCP");

            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
            if (state == ServerStatus.StatusTerminating)
                throw new RsuUpdateVisualsPositionException("Server is terminating");

            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
            if (messageType == ServerMessageType.NoMessage)
            {
                throw new RsuUpdateVisualsPositionException("Server gives wrong message");
            }
            if (messageType != ServerMessageType.VisualPositionUpdate)
            {
                throw new RsuUpdateVisualsPositionException("Server gives wrong message");
            }
            
            ulong numObjects = _tcpHelper.GetData<ulong>();

            for (ulong i = 0; i < numObjects; i++)
            {
                string visualName = _tcpHelper.GetData<string>();
                
                double posX = _tcpHelper.GetData<double>();
                double posY = _tcpHelper.GetData<double>();
                double posZ = _tcpHelper.GetData<double>();
                    
                double quatW = _tcpHelper.GetData<double>();
                double quatX = _tcpHelper.GetData<double>();
                double quatY = _tcpHelper.GetData<double>();
                double quatZ = _tcpHelper.GetData<double>();

                GameObject localObject = GameObject.Find(visualName);

                if (localObject != null)
                {
                    ObjectController.SetTransform(
                        localObject, 
                        new Vector3((float)posX, (float)posY, (float)posZ), 
                        new Quaternion((float)quatX, (float)quatY, (float)quatZ, (float)quatW)
                    );
                }
                else
                {
                    throw new RsuUpdateVisualsPositionException("Cannot find unity game object: " + visualName);
                }
            }
        }

        private void UpdateContacts()
        {
            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestContactInfos));
            if (_tcpHelper.ReadData() <= 0)
                throw new RsuUpdateContactsException("Cannot read data from TCP");
            
            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
            if (state == ServerStatus.StatusTerminating)
                throw new RsuUpdateContactsException("Server is terminating");

            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
            if (messageType != ServerMessageType.ContactInfoUpdate)
            {
                throw new RsuUpdateContactsException("Server gives wrong message");
            }
            
            ulong configurationNumber = _tcpHelper.GetData<ulong>();

            ulong numContacts = _tcpHelper.GetData<ulong>();

            // clear contacts 
            ClearContacts();

            // create contact marker
            List<Tuple<Vector3, Vector3>> contactList = new List<Tuple<Vector3, Vector3>>();
            float forceMaxNorm = 0;

            for (ulong i = 0; i < numContacts; i++)
            {
                double posX = _tcpHelper.GetData<double>();
                double posY = _tcpHelper.GetData<double>();
                double posZ = _tcpHelper.GetData<double>();

                double forceX = _tcpHelper.GetData<double>();
                double forceY = _tcpHelper.GetData<double>();
                double forceZ = _tcpHelper.GetData<double>();
                var force = new Vector3((float) forceX, (float) forceY, (float) forceZ);
                
                contactList.Add(new Tuple<Vector3, Vector3>(
                    new Vector3((float) posX, (float) posY, (float) posZ), force
                ));
                
                forceMaxNorm = Math.Max(forceMaxNorm, force.magnitude);
            }
            
            for (ulong i = 0; i < numContacts; i++)
            {
                var contact = contactList[(int) i];

                if (contact.Item2.magnitude > 0)
                {
                    if(_showContactPoints)
                        _objectController.CreateContactMarker(
                            _contactPointsRoot, (int)i, contact.Item1, _contactPointMarkerScale);

                    if (_showContactForces)
                    {
                        _objectController.CreateContactForceMarker(
                            _contactForcesRoot, (int) i, contact.Item1, contact.Item2 / forceMaxNorm,
                            _contactForceMarkerScale);
                    }
                }
            }
        }
        
        private void ReadXmlString()
        {
            _tcpHelper.WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestConfigXML));
            if (_tcpHelper.ReadData() <= 0)
                throw new RsuReadXMLException("Cannot read data from TCP");
            
            ServerStatus state = _tcpHelper.GetData<ServerStatus>();
            
            if (state == ServerStatus.StatusTerminating)
                throw new RsuReadXMLException("Server is terminating");

            ServerMessageType messageType = _tcpHelper.GetData<ServerMessageType>();
            if (messageType == ServerMessageType.NoMessage) return; // No XML
                
            if (messageType != ServerMessageType.ConfigXml)
            {
                throw new RsuReadXMLException("Server gives wrong message");
            }

            string xmlString = _tcpHelper.GetData<string>();

            XmlDocument xmlDoc = new XmlDocument();
            if (xmlDoc != null)
            {
                xmlDoc.LoadXml(xmlString);
                _xmlReader.CreateApperanceMap(xmlDoc);
            }
        }

        void OnApplicationQuit()
        {
            // close tcp client
            _tcpHelper.CloseConnection();
            
            // save preference
            _loader.SaveToPref();
        }

        public void ShowOrHideObjects()
        {
            // Visual body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Visual))
            {
                foreach (var collider in obj.GetComponentsInChildren<Collider>())
                    collider.enabled = _showVisualBody;
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = _showVisualBody;
                    Color temp = renderer.material.color;
                    if (_showContactForces || _showContactPoints || _showBodyFrames)
                    {
                        renderer.material.shader = _transparentShader;
                        renderer.material.color = new Color(temp.r, temp.g, temp.b, 0.8f);
                    }
                    else
                    {
                        renderer.material.shader = _standardShader;
                        renderer.material.color = new Color(temp.r, temp.g, temp.b, 1.0f);
                    }
                }
            }

            // Collision body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Collision))
            {
                foreach (var col in obj.GetComponentsInChildren<Collider>())
                    col.enabled = _showCollisionBody;
                foreach (var ren in obj.GetComponentsInChildren<Renderer>())
                {
                    ren.enabled = _showCollisionBody;
                    Color temp = ren.material.color;
                    if (_showContactForces || _showContactPoints || _showBodyFrames)
                    {
                        Material mat = ren.material;
                        mat.shader = _transparentShader;
                        mat.color = new Color(temp.r, temp.g, temp.b, 0.5f);
                    }
                    else
                    {
                        Material mat = ren.material;
                        mat.shader = _standardShader;
                        mat.color = new Color(temp.r, temp.g, temp.b, 1.0f);
                    }
                }
            }

            // Contact points
            foreach (Transform contact in _contactPointsRoot.transform)
            {
                contact.gameObject.GetComponent<Renderer>().enabled = _showContactPoints;
            }
            
            // Contact forces
            foreach (Transform contact in _contactForcesRoot.transform)
            {
                contact.gameObject.GetComponentInChildren<Renderer>().enabled = _showContactForces;
            }
            
            // Body frames
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Frame))
            {
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = _showBodyFrames;
                }
            }
        }

        //**************************************************************************************************************
        //  Getter and Setters 
        //**************************************************************************************************************
        
        public bool ShowVisualBody
        {
            get => _showVisualBody;
            set => _showVisualBody = value;
        }

        public bool ShowCollisionBody
        {
            get => _showCollisionBody;
            set => _showCollisionBody = value;
        }

        public bool ShowContactPoints
        {
            get => _showContactPoints;
            set => _showContactPoints = value;
        }

        public bool ShowContactForces
        {
            get => _showContactForces;
            set => _showContactForces = value;
        }

        public bool ShowBodyFrames
        {
            get => _showBodyFrames;
            set => _showBodyFrames = value;
        }

        public float ContactPointMarkerScale
        {
            get => _contactPointMarkerScale;
            set => _contactPointMarkerScale = value;
        }

        public float ContactForceMarkerScale
        {
            get => _contactForceMarkerScale;
            set => _contactForceMarkerScale = value;
        }

        public float BodyFrameMarkerScale
        {
            get => _bodyFrameMarkerScale;
            set
            {
                _bodyFrameMarkerScale = value;
                foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Frame))
                {
                    obj.transform.localScale = new Vector3(0.03f * value, 0.03f * value, 0.1f * value);
                }
            }
        }

        public string TcpAddress
        {
            get => _tcpHelper.TcpAddress;
            set => _tcpHelper.TcpAddress = value;
        }

        public int TcpPort
        {
            get => _tcpHelper.TcpPort;
            set => _tcpHelper.TcpPort = value;
        }
        
        public bool TcpConnected
        {
            get => _tcpHelper.Connected;
        }

        public ResourceLoader ResourceLoader
        {
            get { return _loader; }
        }
    }
}