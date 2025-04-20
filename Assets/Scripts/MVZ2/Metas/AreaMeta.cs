using System.Collections.Generic;
using System.Xml;
using MVZ2.IO;
using MVZ2Logic;
using MVZ2Logic.Level;
using PVZEngine;
using UnityEngine; // 若需日志，需引入

namespace MVZ2.Metas
{
    public class AreaMeta : IAreaMeta
    {
        public string ID { get; private set; }
        public NamespaceID ModelID { get; private set; }
        public NamespaceID MusicID { get; private set; }
        public NamespaceID Cart { get; private set; }
        public NamespaceID[] Tags { get; private set; }
        public SpriteReference StarshardIcon { get; private set; }

        public float EnemySpawnX { get; private set; }
        public float DoorZ { get; private set; }

        public float NightValue { get; private set; }

        public float GridWidth { get; private set; }
        public float GridHeight { get; private set; }
        public float GridLeftX { get; private set; }
        public float GridBottomZ { get; private set; }
        public int Lanes { get; private set; }
        public int Columns { get; private set; }

        public AreaGrid[] Grids { get; private set; }
        IAreaGridMeta[] IAreaMeta.Grids => Grids;

        public static AreaMeta FromXmlNode(XmlNode node, string defaultNsp, string currentStageId = null)
        {
            var id = node.GetAttribute("id");
            var model = node.GetAttributeNamespaceID("model", defaultNsp);
            var music = node.GetAttributeNamespaceID("music", defaultNsp);
            var cart = node.GetAttributeNamespaceID("cart", defaultNsp);
            var starshard = node.GetAttributeSpriteReference("starshard", defaultNsp);
            var tags = node.GetAttributeNamespaceIDArray("tags", defaultNsp);

            float enemySpawnX = 1080;
            float doorZ = 240;
            var positionsNode = node["positions"];
            if (positionsNode != null)
            {
                enemySpawnX = positionsNode.GetAttributeFloat("enemySpawnX") ?? enemySpawnX;
                doorZ = positionsNode.GetAttributeFloat("doorZ") ?? doorZ;
            }

            float nightValue = 0;
            var lightingNode = node["lighting"];
            if (lightingNode != null)
            {
                nightValue = lightingNode.GetAttributeFloat("night") ?? 0;
            }

            //--- 核心修改：Grids 匹配逻辑 ---
            float gridWidth = 80;
            float gridHeight = 80;
            float leftX = 260;
            float bottomZ = 80;
            int lanes = 5;
            int columns = 9;
            List<AreaGrid> grids = new List<AreaGrid>();

            // 1. 收集所有符合条件的 grids 节点
            List<XmlNode> matchedGrids = new List<XmlNode>();
            XmlNode defaultGrids = null;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "grids")
                {
                    string condition = child.GetAttribute("condition");
                    if (condition == currentStageId)
                    {
                        matchedGrids.Add(child);
                    }
                    else if (condition == "default" && defaultGrids == null)
                    {
                        defaultGrids = child;
                    }
                }
            }

            // 2. 选择优先级：最后一个匹配的 grids > default
            XmlNode gridsNode = null;
            if (matchedGrids.Count > 0)
            {
                gridsNode = matchedGrids[matchedGrids.Count - 1];
                Debug.Log($"AreaMeta [{id}] 使用 StageID 匹配的 grids: {currentStageId}");
            }
            else if (defaultGrids != null)
            {
                gridsNode = defaultGrids;
                Debug.Log($"AreaMeta [{id}] 使用默认 grids");
            }

            // 3. 解析选中的 grids 节点
            if (gridsNode != null)
            {
                gridWidth = gridsNode.GetAttributeFloat("width") ?? gridWidth;
                gridHeight = gridsNode.GetAttributeFloat("height") ?? gridHeight;
                leftX = gridsNode.GetAttributeFloat("leftX") ?? leftX;
                bottomZ = gridsNode.GetAttributeFloat("bottomZ") ?? bottomZ;
                lanes = gridsNode.GetAttributeInt("lanes") ?? lanes;
                columns = gridsNode.GetAttributeInt("columns") ?? columns;

                foreach (XmlNode childNode in gridsNode.ChildNodes)
                {
                    if (childNode.Name == "grid")
                    {
                        grids.Add(AreaGrid.FromXmlNode(childNode, defaultNsp));
                    }
                }
            }
            else
            {
                Debug.LogWarning($"AreaMeta [{id}] 未找到任何 grids 配置！");
            }

            return new AreaMeta()
            {
                ID = id,
                ModelID = model,
                MusicID = music,
                StarshardIcon = starshard,
                Cart = cart,
                Tags = tags,

                EnemySpawnX = enemySpawnX,
                DoorZ = doorZ,

                NightValue = nightValue,

                GridWidth = gridWidth,
                GridHeight = gridHeight,
                GridLeftX = leftX,
                GridBottomZ = bottomZ,
                Lanes = lanes,
                Columns = columns,
                Grids = grids.ToArray()
            };
        }
    }

    public class AreaGrid : IAreaGridMeta
    {
        public NamespaceID ID { get; set; }
        public float YOffset { get; set; }
        public SpriteReference Sprite { get; set; }
        public float Slope { get; set; }

        public static AreaGrid FromXmlNode(XmlNode node, string defaultNsp)
        {
            var id = node.GetAttributeNamespaceID("id", defaultNsp);
            var yOffset = node.GetAttributeFloat("yOffset") ?? 0;
            var sprite = node.GetAttributeSpriteReference("sprite", defaultNsp);
            var slope = node.GetAttributeFloat("slope") ?? 0;
            return new AreaGrid()
            {
                ID = id,
                YOffset = yOffset,
                Sprite = sprite,
                Slope = slope
            };
        }
    }
}