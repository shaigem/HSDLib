﻿using HSDLib.Common;
using HSDLib.Animation;
using HSDLib.MaterialAnimation;
using HSDLib.KAR;
using HSDLib.Melee.PlData;
using HSDLib.Melee;

namespace HSDLib
{
    /// <summary>
    /// A root of the hsd structure
    /// Contains a single HSDNode and a Name
    /// The type of node depends on the file type
    /// </summary>
    public class HSDRoot : IHSDNode
    {
        public string Name;
        public IHSDNode Node;

        public uint Offset;
        public uint NameOffset;

        public override void Open(HSDReader Reader)
        {
            // Assuming Node Type from the root name
            if (Name.EndsWith("matanim_joint"))
            {
                Node = Reader.ReadObject<HSD_MatAnimJoint>(Offset);
            }
            else if (Name.EndsWith("_joint"))
            {
                Node = Reader.ReadObject<HSD_JOBJ>(Offset);
            }
            else if (Name.EndsWith("_figatree"))
            {
                Node = Reader.ReadObject<HSD_FigaTree>(Offset);
            }
            else if (Name.StartsWith("vcDataStar") || Name.StartsWith("vcDataWing"))
            {
                Node = Reader.ReadObject<KAR_VcStarVehicle>(Offset);
            }
            else if (Name.StartsWith("vcDataWheel"))
            {
                Node = Reader.ReadObject<KAR_WheelVehicle>(Offset);
            }
            else if (Name.StartsWith("grModelMotion"))
            {
                Node = Reader.ReadObject<KAR_GrModelMotion>(Offset);
            }
            else if (Name.StartsWith("grModel"))
            {
                Node = Reader.ReadObject<KAR_GrModel>(Offset);
            }
            else if (Name.StartsWith("grData"))
            {
                Node = Reader.ReadObject<KAR_GrData>(Offset);
            }
            else if (Name.StartsWith("ftData"))
            {
                Node = Reader.ReadObject<SBM_FighterData>(Offset);
            }
            else if (Name.StartsWith("grGroundParam"))
            {
                Node = Reader.ReadObject<SBM_GrGroundParam>(Offset);
            }
            else if (Name.StartsWith("coll_data"))
            {
                Node = Reader.ReadObject<SBM_GrCollData>(Offset);
            }
            else if (Name.StartsWith("map_plit"))
            {
                Node = Reader.ReadObject<SBM_GrMapPLIT>(Offset);
            }
            else if (Name.StartsWith("ALDYakuAll"))
            {
                Node = Reader.ReadObject<SBM_GrYakuAll>(Offset);
            }
            else if (Name.StartsWith("map_head"))
            {
                Node = Reader.ReadObject<SBM_GrMapHead>(Offset);
            }
            else if (Name.StartsWith("map_texg"))
            {
                Node = Reader.ReadObject<SBM_GrMapTexG>(Offset);
            }
            else if (Name.StartsWith("Sc"))
            {
                Node = Reader.ReadObject<HSD_SOBJ>(Offset);
            }
        }

        public override void Save(HSDWriter Writer)
        {
            // a little wonky to have to go through the saving process 3 times
            // but this does result in smaller filesizes due to data alignment

            // the correct order is probably buffer->images->jobjweights->attributegroups->dobj/pobj->jobjs
            // but it probably doesn't matter?
            if (Node == null)
                return;
            
            Node.Save(Writer);
        }
    }
}
