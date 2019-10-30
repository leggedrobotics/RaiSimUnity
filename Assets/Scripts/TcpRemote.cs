/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using SimpleFileBrowser;

namespace raisimUnity
{
    // socket commands from client
    enum ServerMessageType : int
    {
        Initialization = 0,
        ObjectPositionUpdate,
        Status,
        NoMessage,
        ContactInfoUpdate,
        ConfigXml,
    }

    enum ClientMessageType : int
    {    
        RequestObjectPosition = 0,
        RequestInitialization,
        RequestResource,                 // request mesh, texture. etc files
        RequestChangeRealtimeFactor,
        RequestContactSolverDetails,
        RequestPause,
        RequestResume,
        RequestContactInfos,
        RequestConfigXML,
    }

    enum ServerStatus : int
    {
        StatusRendering = 0,
        StatusHibernating,
        StatusTerminating,
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

    static class VisualTag
    {
        public const string Visual = "visual";
        public const string Collision = "collision";
    }

    public class TcpRemote : MonoBehaviour
    {
        // prevent repeated instances
        private static TcpRemote instance;

        private TcpRemote()
        {
            // xml 
            _xmlReader = new XmlReader();
            
            // resource loader
            _loader = new ResourceLoader();
        }
        
        public static TcpRemote Instance
        {
            get 
            {
                if( instance==null )
                {
                    instance = new TcpRemote();
                    return instance;
                }
                else
                    throw new System.Exception("TCPRemote can only be instantiated once");
            }
        }
        
        // script options
        private string _tcpAddress = "127.0.0.1";
        private int _tcpPort = 8080;

        // tcp client and stream
        private TcpClient _client = null;
        private NetworkStream _stream = null;
        
        // buffer
        // TODO get buffer spec from raisim?
        private const int _maxBufferSize = 33554432;
        private const int _maxPacketSize = 4096;
        private const int _footerSize = sizeof(Byte);
        
        private byte[] _buffer;
        
        // status
        private bool _tcpTryConnect = false;
        private bool _showVisualBody = true;
        private bool _showCollisionBody = false;
        private bool _showContactPoints = false;
        private bool _showContactForces = false;
        
        // root objects
        private GameObject _objectsRoot;
        private GameObject _contactPointsRoot;
        private GameObject _contactForcesRoot;
        
        // object controller 
        private ObjectController _objectController;

        // shaders
        private Shader _transparentShader;
        private Shader _standardShader;
        
        // default materials
        private Material _planeMaterial;
        private Material _terrainMaterial;
        private Material _defaultMaterialR;
        private Material _defaultMaterialG;
        private Material _defaultMaterialB;
        
        // xml document
        private XmlReader _xmlReader;
        
        // resource loader
        private ResourceLoader _loader;
        
        void Start()
        {
            // set buffer size
            _buffer = new byte[_maxBufferSize];
            
            // object roots
            _objectsRoot = GameObject.Find("Objects");
            _contactPointsRoot = GameObject.Find("ContactPoints");
            _contactForcesRoot = GameObject.Find("ContactForces");
            
            // object controller 
            _objectController = new ObjectController();

            // shaders
            _standardShader = Shader.Find("Standard");
            _transparentShader = Shader.Find("RaiSim/Transparent");
            
            // materials
            _planeMaterial = Resources.Load<Material>("Tiles1");
            _terrainMaterial = Resources.Load<Material>("Ground1");
            _defaultMaterialR = Resources.Load<Material>("Plastic1");
            _defaultMaterialG = Resources.Load<Material>("Plastic2");
            _defaultMaterialB = Resources.Load<Material>("Plastic3");
        }

        void Update()
        {
            // broken connection: clear
            if( !CheckConnection() )
            {
                // TODO connection lost popup
                ClearScene();
                
                _client = null;
                _stream = null;
            }

            // data available: handle communication
            if (_client != null && _client.Connected && _stream != null)
            {
                try
                {
                    // update object position
                    UpdatePosition();

                    // update contacts
                    UpdateContacts();
                }
                catch (Exception e)
                {
                    print("update failed." + e);
                }
            }
            
            // escape
            if(Input.GetKey("escape"))
                Application.Quit();
        }

        private void ClearScene()
        {
            // TODO maybe we can just clear RSUnity children?
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
            
            // clear appearances
            if(_xmlReader != null)
                _xmlReader.ClearAppearanceMap();
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

        private int InitializeScene()
        {
            int offset = 0;
            
            WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestInitialization));
            if (ReadData() == 0)
                return -1;

            ServerStatus state = BitIO.GetData<ServerStatus>(ref _buffer, ref offset);
            
            if (state == ServerStatus.StatusTerminating)
                return 0;

            ServerMessageType messageType = BitIO.GetData<ServerMessageType>(ref _buffer, ref offset);

            ulong configurationNumber = BitIO.GetData<ulong>(ref _buffer, ref offset);

            ulong numObjects = BitIO.GetData<ulong>(ref _buffer, ref offset);

            for (ulong i = 0; i < numObjects; i++)
            {
                ulong objectIndex = BitIO.GetData<ulong>(ref _buffer, ref offset);
                
                RsObejctType objectType = BitIO.GetData<RsObejctType>(ref _buffer, ref offset);
                
                // get name and find corresponding appearance from XML
                string name = BitIO.GetData<string>(ref _buffer, ref offset);
                Appearances? appearances = _xmlReader.FindApperancesFromObjectName(name);
                
                if (objectType == RsObejctType.RsArticulatedSystemObject)
                {
                    string urdfDirPathInServer = BitIO.GetData<string>(ref _buffer, ref offset); 

                    // visItem = 0 (visuals)
                    // visItem = 1 (collisions)
                    for (int visItem = 0; visItem < 2; visItem++)
                    {
                        ulong numberOfVisObjects = BitIO.GetData<ulong>(ref _buffer, ref offset);

                        for (ulong j = 0; j < numberOfVisObjects; j++)
                        {
                            RsShapeType shapeType = BitIO.GetData<RsShapeType>(ref _buffer, ref offset);
                                
                            ulong group = BitIO.GetData<ulong>(ref _buffer, ref offset);

                            string subName = Path.Combine(objectIndex.ToString(), visItem.ToString(), j.ToString());
                            
                            var objFrame = new GameObject(subName);
                            objFrame.transform.SetParent(_objectsRoot.transform, false);

                            string tag = "";
                            if (visItem == 0)
                                tag = VisualTag.Visual;
                            else if (visItem == 1)
                                tag = VisualTag.Collision;

                            if (shapeType == RsShapeType.RsMeshShape)
                            {
                                string meshFile = BitIO.GetData<string>(ref _buffer, ref offset);
                                string meshFileExtension = Path.GetExtension(meshFile);

                                double sx = BitIO.GetData<double>(ref _buffer, ref offset);
                                double sy = BitIO.GetData<double>(ref _buffer, ref offset);
                                double sz = BitIO.GetData<double>(ref _buffer, ref offset);

                                string meshFilePathInResourceDir = _loader.RetrieveMeshPath(urdfDirPathInServer, meshFile);
                                var mesh = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, (float)sx, (float)sy, (float)sz, meshFileExtension != ".dae");
                                mesh.tag = tag;
                            }
                            else
                            {
                                ulong size = BitIO.GetData<ulong>(ref _buffer, ref offset);
                                    
                                var visParam = new List<double>();
                                for (ulong k = 0; k < size; k++)
                                {
                                    double visSize = BitIO.GetData<double>(ref _buffer, ref offset);
                                    visParam.Add(visSize);
                                }
                                switch (shapeType)
                                {
                                    case RsShapeType.RsBoxShape:
                                    {
                                        if (visParam.Count != 3) throw new Exception("Box Mesh error");
                                        var box = _objectController.CreateBox(objFrame, (float) visParam[0], (float) visParam[1], (float) visParam[2]);
                                        box.tag = tag;
                                    }
                                        break;
                                    case RsShapeType.RsCapsuleShape:
                                    {
                                        if (visParam.Count != 2) throw new Exception("Capsule Mesh error");
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
                                        if (visParam.Count != 2) throw new Exception("Cylinder Mesh error");
                                        var cylinder = _objectController.CreateCylinder(objFrame, (float)visParam[0], (float)visParam[1]);
                                        cylinder.tag = tag;
                                    }
                                        break;
                                    case RsShapeType.RsSphereShape:
                                    {
                                        if (visParam.Count != 1) throw new Exception("Sphere Mesh error");
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
                    
                    float height = BitIO.GetData<float>(ref _buffer, ref offset);
                    var objFrame = new GameObject(objectIndex.ToString());
                    objFrame.transform.SetParent(_objectsRoot.transform, false);
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
                    float centerX = BitIO.GetData<float>(ref _buffer, ref offset);
                    float centerY = BitIO.GetData<float>(ref _buffer, ref offset);
                    // size
                    float sizeX = BitIO.GetData<float>(ref _buffer, ref offset);
                    float sizeY = BitIO.GetData<float>(ref _buffer, ref offset);
                    // num samples
                    ulong numSampleX = BitIO.GetData<ulong>(ref _buffer, ref offset);
                    ulong numSampleY = BitIO.GetData<ulong>(ref _buffer, ref offset);
                    ulong numSample = BitIO.GetData<ulong>(ref _buffer, ref offset);
                        
                    // height values 
                    float[,] heights = new float[numSampleY, numSampleX];
                    for (ulong j = 0; j < numSampleY; j++)
                    {
                        for (ulong k = 0; k < numSampleX; k++)
                        {
                            float height = BitIO.GetData<float>(ref _buffer, ref offset);
                            heights[j, k] = height;
                        }
                    }

                    var objFrame = new GameObject(objectIndex.ToString());
                    objFrame.transform.SetParent(_objectsRoot.transform, false);
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
                    var objFrame = new GameObject(objectIndex.ToString());
                    objFrame.transform.SetParent(_objectsRoot.transform, false);
                    
                    // get material
                    Material material;
                    if (appearances != null && !string.IsNullOrEmpty(appearances.As<Appearances>().materialName))
                        material = Resources.Load<Material>(appearances.As<Appearances>().materialName);
                    else
                    {
                        // default material
                        switch (i % 3)
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
                    switch (objectType) 
                    {
                        case RsObejctType.RsSphereObject :
                        {
                            float radius = BitIO.GetData<float>(ref _buffer, ref offset);
                            var sphere =  _objectController.CreateSphere(objFrame, radius);
                            sphere.GetComponentInChildren<Renderer>().material = material;
                            sphere.tag = VisualTag.Collision;
                        }
                            break;

                        case RsObejctType.RsBoxObject :
                        {
                            float sx = BitIO.GetData<float>(ref _buffer, ref offset);
                            float sy = BitIO.GetData<float>(ref _buffer, ref offset);
                            float sz = BitIO.GetData<float>(ref _buffer, ref offset);
                            var box = _objectController.CreateBox(objFrame, sx, sy, sz);
                            box.GetComponentInChildren<Renderer>().material = material;
                            box.tag = VisualTag.Collision;
                        }
                            break;
                        case RsObejctType.RsCylinderObject:
                        {
                            float radius = BitIO.GetData<float>(ref _buffer, ref offset);
                            float height = BitIO.GetData<float>(ref _buffer, ref offset);
                            var cylinder = _objectController.CreateCylinder(objFrame, radius, height);
                            cylinder.GetComponentInChildren<Renderer>().material = material;
                            cylinder.tag = VisualTag.Collision;
                        }
                            break;
                        case RsObejctType.RsCapsuleObject:
                        {
                            float radius = BitIO.GetData<float>(ref _buffer, ref offset);
                            float height = BitIO.GetData<float>(ref _buffer, ref offset);
                            var capsule = _objectController.CreateCapsule(objFrame, radius, height);
                            capsule.GetComponentInChildren<Renderer>().material = material;
                            capsule.tag = VisualTag.Collision;
                        }
                            break;
                        case RsObejctType.RsMeshObject:
                        {
                            string meshFile = BitIO.GetData<string>(ref _buffer, ref offset);
                            float scale = BitIO.GetData<float>(ref _buffer, ref offset);
                            
                            string meshFileName = Path.GetFileName(meshFile);       
                            string meshFileExtension = Path.GetExtension(meshFile);
                            
                            string meshFilePathInResourceDir = _loader.RetrieveMeshPath(Path.GetDirectoryName(meshFile), meshFileName);
                            
                            var mesh = _objectController.CreateMesh(objFrame, meshFilePathInResourceDir, 
                                scale, scale, scale, meshFileExtension != ".dae");
                            mesh.GetComponentInChildren<Renderer>().material = material;
                            mesh.tag = VisualTag.Collision;
                        }
                            break;
                    }
                    
                    // visual body
                    if (appearances != null)
                    {
                        foreach (var subapp in appearances.As<Appearances>().subAppearances)
                        {
                            switch (subapp.shapes)
                            {
                                case AppearanceShapes.Sphere:
                                {
                                    float radius = subapp.dimension.x;
                                    var sphere =  _objectController.CreateSphere(objFrame, radius);
                                    sphere.GetComponentInChildren<Renderer>().material = material;
                                    sphere.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Box:
                                {
                                    var box = _objectController.CreateBox(objFrame, subapp.dimension.x, subapp.dimension.y, subapp.dimension.z);
                                    box.GetComponentInChildren<Renderer>().material = material;
                                    box.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Cylinder:
                                {
                                    var cylinder = _objectController.CreateCylinder(objFrame, subapp.dimension.x, subapp.dimension.y);
                                    cylinder.GetComponentInChildren<Renderer>().material = material;
                                    cylinder.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Capsule:
                                {
                                    var capsule = _objectController.CreateCapsule(objFrame, subapp.dimension.x, subapp.dimension.y);
                                    capsule.GetComponentInChildren<Renderer>().material = material;
                                    capsule.tag = VisualTag.Visual;
                                }
                                    break;
                                case AppearanceShapes.Mesh:
                                {
                                    string meshFile = subapp.fileName;
                                    string meshFileName = Path.GetFileNameWithoutExtension(meshFile);
                                    string meshFileExtension = Path.GetExtension(meshFile);
                                    string directoryName = Path.GetFileName(Path.GetDirectoryName(meshFile));
                                    var mesh = _objectController.CreateMesh(objFrame, Path.Combine(directoryName, meshFileName), subapp.dimension.x, subapp.dimension.x, subapp.dimension.x, 
                                        meshFileExtension != ".dae");
                                    mesh.GetComponentInChildren<Renderer>().material = material;
                                    mesh.tag = VisualTag.Visual;
                                }
                                    break;
                                default:
                                    throw new NotImplementedException("Not Implemented Appearance Shape");
                                    break;
                            }
                        }
                    }
                }
            }
            
            Array.Clear(_buffer, 0, _maxBufferSize);
            return 0;
        }
        
        private int UpdatePosition()
        {
            int offset = 0;
            
            WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestObjectPosition));
            if (ReadData() == 0)
                return -1;

            ServerStatus state = BitIO.GetData<ServerStatus>(ref _buffer, ref offset);
            
            if (state == ServerStatus.StatusTerminating)
                return 0;

            ServerMessageType messageType = BitIO.GetData<ServerMessageType>(ref _buffer, ref offset);
            if (messageType == ServerMessageType.NoMessage)
            {
                return -1;
            }
            
            ulong configurationNumber = BitIO.GetData<ulong>(ref _buffer, ref offset);

            ulong numObjects = BitIO.GetData<ulong>(ref _buffer, ref offset);

            for (ulong i = 0; i < numObjects; i++)
            {
                ulong localIndexSize = BitIO.GetData<ulong>(ref _buffer, ref offset);

                for (ulong j = 0; j < localIndexSize; j++)
                {
                    string objectName = BitIO.GetData<string>(ref _buffer, ref offset);
                    
                    double posX = BitIO.GetData<double>(ref _buffer, ref offset);
                    double posY = BitIO.GetData<double>(ref _buffer, ref offset);
                    double posZ = BitIO.GetData<double>(ref _buffer, ref offset);
                    
                    double quatW = BitIO.GetData<double>(ref _buffer, ref offset);
                    double quatX = BitIO.GetData<double>(ref _buffer, ref offset);
                    double quatY = BitIO.GetData<double>(ref _buffer, ref offset);
                    double quatZ = BitIO.GetData<double>(ref _buffer, ref offset);

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
                        // TODO error
                        print("local object is null");
                    }
                }
            }
            
            Array.Clear(_buffer, 0, _maxBufferSize);
            return 0;
        }

        private int UpdateContacts()
        {
            int offset = 0;
            
            WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestContactInfos));
            if (ReadData() == 0)
                return -1;
            
            ServerStatus state = BitIO.GetData<ServerStatus>(ref _buffer, ref offset);
            
            if (state == ServerStatus.StatusTerminating)
                return 0;

            ServerMessageType messageType = BitIO.GetData<ServerMessageType>(ref _buffer, ref offset);
            if (messageType != ServerMessageType.ContactInfoUpdate)
            {
                return -1;
            }
            
            ulong configurationNumber = BitIO.GetData<ulong>(ref _buffer, ref offset);

            ulong numContacts = BitIO.GetData<ulong>(ref _buffer, ref offset);

            // clear contacts 
            ClearContacts();

            // create contact marker
            for (ulong i = 0; i < numContacts; i++)
            {
                double posX = BitIO.GetData<double>(ref _buffer, ref offset);
                double posY = BitIO.GetData<double>(ref _buffer, ref offset);
                double posZ = BitIO.GetData<double>(ref _buffer, ref offset);
                
                if(_showContactPoints)
                    _objectController.CreateContactMarker(_contactPointsRoot, (int)i, new Vector3((float)posX, (float)posY, (float)posZ));
                
                double forceX = BitIO.GetData<double>(ref _buffer, ref offset);
                double forceY = BitIO.GetData<double>(ref _buffer, ref offset);
                double forceZ = BitIO.GetData<double>(ref _buffer, ref offset);
                
                if(_showContactForces)
                    _objectController.CreateContactForceMarker(_contactForcesRoot, (int)i, 
                        new Vector3((float)posX, (float)posY, (float)posZ), 
                        new Vector3((float)forceX, (float)forceY, (float)forceZ));
            }

            return 0;
        }
        
        public void EstablishConnection()
        {
            try
            {
                // create tcp client and stream
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient(_tcpAddress, _tcpPort);
                    _stream = _client.GetStream();
                }
            }
            catch (Exception e)
            {
                // connection cannot be established
                throw new RsuTcpConnectionException(e.Message);
            }

            try
            {
                // initialize scene when connection available
                if (_client != null && _client.Connected && _stream != null)
                {
                    // Read XML string
                    if (ReadXMLString() != 0)
                    {
                        // TODO error
                    }

                    // initialize scene from data 
                    if (InitializeScene() != 0)
                    {
                        // TODO error
                    }
                    
                    // disable other cameras than main camera
                    foreach (var cam in Camera.allCameras)
                    {
                        if (cam == Camera.main) continue;
                        cam.enabled = false;
                    }
                    
                    // show / hide objects
                    ShowOrHideObjects();
                }
            }
            catch (Exception e)
            {
                // connection cannot be established
                throw new RsuInitException(e.Message);
            }
        }

        private int ReadXMLString()
        {
            int offset = 0;
            
            WriteData(BitConverter.GetBytes((int) ClientMessageType.RequestConfigXML));
            if (ReadData() == 0)
                return -1;
            
            ServerStatus state = BitIO.GetData<ServerStatus>(ref _buffer, ref offset);
            
            if (state == ServerStatus.StatusTerminating)
                return 0;

            ServerMessageType messageType = BitIO.GetData<ServerMessageType>(ref _buffer, ref offset);
            if (messageType == ServerMessageType.NoMessage)
            {
                return 0;
            }
            else if (messageType != ServerMessageType.ConfigXml)
            {
                return -1;
            }

            string xmlString = BitIO.GetData<string>(ref _buffer, ref offset);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            _xmlReader.CreateApperanceMap(xmlDoc);

            return 0;
        }

        public void CloseConnection()
        {
            try
            {
                // clear scene
                ClearScene();
                
                // clear tcp stream and client
                if (_stream != null)
                {
                    _stream.Close();
                    _stream = null;
                }

                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
            catch (Exception)
            {
            }
        }
        
        private bool CheckConnection()
        {
            try
            {
                if( _client!=null && _client.Client!=null && _client.Client.Connected )
                {
                    if( _client.Client.Poll(0, SelectMode.SelectRead) )
                    {
                        if( _client.Client.Receive(_buffer, SocketFlags.Peek)==0 )
                            return false;
                        else
                            return true;
                    }
                    else
                        return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        
        private int ReadData()
        {
            while (!_stream.DataAvailable)
            {
                // wait until stream data is available
                // TODO what to do when it is stucked here....
            }
            
            int offset = 0;
            Byte footer = Convert.ToByte('c');
            while (footer == Convert.ToByte('c'))
            {
                int valread = _stream.Read(_buffer, offset, _maxPacketSize);
                if (valread == 0) break;
                footer = _buffer[offset + _maxPacketSize - _footerSize];
                offset += valread - _footerSize;
            }
            return offset;
        }

        private int WriteData(Byte[] data)
        {
            while (!_stream.CanWrite)
            {
                // wait until stream can write
                // TODO what to do when it is stucked here....
            }
            
            _stream.Write(data, 0, data.Length);
            return 0;
        }
        
        void OnApplicationQuit()
        {
            // close tcp client
            if (_stream != null) _stream.Close();
            if (_client != null) _client.Close();
            
            // save preference
            _loader.SaveResourceDirectories();
        }

        public void ShowOrHideObjects()
        {
            // visual body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Visual))
            {
                foreach (var collider in obj.GetComponentsInChildren<Collider>())
                    collider.enabled = _showVisualBody;
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = _showVisualBody;
                    Color temp = renderer.material.color;
                    if (_showContactForces || _showContactPoints)
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

            // collision body
            foreach (var obj in GameObject.FindGameObjectsWithTag(VisualTag.Collision))
            {
                foreach (var collider in obj.GetComponentsInChildren<Collider>())
                    collider.enabled = _showCollisionBody;
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = _showCollisionBody;
                    Color temp = renderer.material.color;
                    if (_showContactForces || _showContactPoints)
                    {
                        renderer.material.shader = _transparentShader;
                        renderer.material.color = new Color(temp.r, temp.g, temp.b, 0.5f);
                    }
                    else
                    {
                        renderer.material.shader = _standardShader;
                        renderer.material.color = new Color(temp.r, temp.g, temp.b, 1.0f);
                    }
                }
            }

            // contact points
            foreach (Transform contact in _contactPointsRoot.transform)
            {
                contact.gameObject.GetComponent<Renderer>().enabled = _showContactPoints;
            }
            
            // contact forces
            foreach (Transform contact in _contactForcesRoot.transform)
            {
                contact.gameObject.GetComponentInChildren<Renderer>().enabled = _showContactForces;
            }
        }

        // getters and setters
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

        public string TcpAddress
        {
            get => _tcpAddress;
            set => _tcpAddress = value;
        }

        public int TcpPort
        {
            get => _tcpPort;
            set => _tcpPort = value;
        }

        public bool TcpTryConnect
        {
            get => _tcpTryConnect;
            set => _tcpTryConnect = value;
        }

        public bool TcpConnected
        {
            get => _client != null && _client.Connected;
        }

        public ResourceLoader ResourceLoader
        {
            get { return _loader; }
        }
    }
}