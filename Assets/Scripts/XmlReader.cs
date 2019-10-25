using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace raisimUnity
{
    public enum AppearanceShapes : int
    {
        Sphere = 0,
        Box,
        Cylinder,
        Capsule,
        Mesh,
    }
    
    public struct Appearance
    {
        public AppearanceShapes shapes;
        public Vector3 dimension;
        public string fileName;
        public string materialName;
    }

    public struct Appearances
    {
        // each raisim object can have multiple appearances.
        public string materialName;
        public List<Appearance> subAppearances;
    }

    public class XmlReader
    {
        // (name and List<appearance>) 
        private Dictionary<string, Appearances> _table;

        public XmlReader()
        {
            _table = new Dictionary<string, Appearances>();
        }

        public void CreateApperanceMap(XmlDocument xmlDocument)
        {
            // find appearance element under object
            var objects = xmlDocument.DocumentElement.SelectSingleNode("/raisim/world/objects");

            foreach (XmlNode obj in objects.ChildNodes)
            {
                // xml <appearance> tag
                var appearanceNode = obj.SelectSingleNode("appearance");
                if (appearanceNode == null) continue;
                
                Appearances appearances = new Appearances();
                appearances.subAppearances = new List<Appearance>();
                var material = appearanceNode.Attributes["material"];
                if (material != null) appearances.materialName = material.Value;

                // each subappearance
                foreach (XmlNode app in appearanceNode.ChildNodes)
                {
                    Appearance appearance = new Appearance();
                    
                    var matPerAppearance = app.Attributes["material"];
                    if (matPerAppearance != null) appearance.materialName = matPerAppearance.Value;
                    
                    appearances.subAppearances.Add(appearance);
                }
                
                var name = obj.Attributes["name"].Value;
                _table.Add(name, appearances);
            }
        }

        public void ClearAppearanceMap()
        {
            _table.Clear();
        }

        public Appearances? FindApperancesFromObjectName(string name)
        {
            if (!_table.ContainsKey(name)) return null;
            return _table[name];
        }
    }
}