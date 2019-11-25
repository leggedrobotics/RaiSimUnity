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
        VisualInitialization,
        VisualPositionUpdate,
    }

    enum ClientMessageType : int
    {    
        RequestObjectPosition = 0,
        RequestInitializeObjects,
        RequestResource,                 // request mesh, texture. etc files
        RequestChangeRealtimeFactor,
        RequestContactSolverDetails,
        RequestPause,
        RequestResume,
        RequestContactInfos,
        RequestConfigXML,
        RequestInitializeVisuals,
        RequestVisualPosition,
    }

    enum ServerStatus : int
    {
        StatusRendering = 0,
        StatusHibernating,
        StatusTerminating,
    }
}