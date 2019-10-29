/*
 * Author: Dongho Kang (kangd@ethz.ch)
 */

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
                    var shape = app.Name;
                    
                    var matPerAppearance = app.Attributes["material"];
                    if (matPerAppearance != null) appearance.materialName = matPerAppearance.Value;

                    switch (shape)
                    {
                        case "sphere":
                        {
                            appearance.shapes = AppearanceShapes.Sphere;
                            var radius = app.Attributes["radius"];
                            if (radius == null)
                            {
                                // TODO error
                            }
                            appearance.dimension = new Vector3(float.Parse(radius.Value), 0, 0);
                        }
                        break;
                        case "box":
                        {
                            appearance.shapes = AppearanceShapes.Box;
                            var xlength = app.Attributes["xLength"];
                            if (xlength == null)
                            {
                                // TODO error
                            }
                            var ylength = app.Attributes["yLength"];
                            if (ylength == null)
                            {
                                // TODO error
                            }
                            var zlength = app.Attributes["zLength"];
                            if (zlength == null)
                            {
                                // TODO error
                            }
                            
                            appearance.dimension = new Vector3(float.Parse(xlength.Value), float.Parse(ylength.Value), float.Parse(zlength.Value));
                        }
                        break;
                        case "cylinder":
                        {
                            appearance.shapes = AppearanceShapes.Cylinder;
                            var radius = app.Attributes["radius"];
                            if (radius == null)
                            {
                                // TODO error
                            }
                            var length = app.Attributes["length"];
                            if (length == null)
                            {
                                // TODO error
                            }
                            appearance.dimension = new Vector3(float.Parse(radius.Value), float.Parse(length.Value), 0);
                        }
                        break;
                        case "capsule":
                        {
                            appearance.shapes = AppearanceShapes.Capsule;
                            var radius = app.Attributes["radius"];
                            if (radius == null)
                            {
                                // TODO error
                            }
                            var length = app.Attributes["length"];
                            if (length == null)
                            {
                                // TODO error
                            }
                            appearance.dimension = new Vector3(float.Parse(radius.Value), float.Parse(length.Value), 0);
                        }
                        break;
                        case "mesh":
                        {
                            appearance.shapes = AppearanceShapes.Mesh;
                            var scale = app.Attributes["scale"];
                            if (scale == null)
                            {
                                // TODO error
                            }
                            appearance.dimension = new Vector3(float.Parse(scale.Value), 0, 0);
                        }
                        break;
                      default:
                            break;
                    }
                    
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