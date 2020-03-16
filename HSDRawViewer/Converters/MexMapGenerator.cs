﻿using HSDRaw;
using HSDRaw.Common;
using HSDRaw.Common.Animation;
using HSDRaw.Melee.Mn;
using HSDRaw.MEX.Stages;
using HSDRaw.Tools;
using System.Collections.Generic;
using System.Linq;

namespace HSDRawViewer.Converters
{
    public enum MexMapAnimType
    {
        None,
        SlideInFromLeft,
        SlideInFromRight,
        SlideInFromTop,
        SlideInFromBottom,
        GrowFromNothing,
        SpinIn,
        FlipIn
    }

    public class MexMapSpace
    {
        public HSD_TOBJ TOBJ;
        public HSD_JOBJ JOBJ = new HSD_JOBJ() { SX = 1, SY = 1, SZ = 1, Flags = JOBJ_FLAG.CLASSICAL_SCALING };

        public float X { get => JOBJ.TX; set => JOBJ.TX = value; }
        public float Y { get => JOBJ.TY; set => JOBJ.TY = value; }
        public float SX { get => JOBJ.SX; set => JOBJ.SX = value; }
        public float SY { get => JOBJ.SY; set => JOBJ.SY = value; }

        public MexMapAnimType AnimType = MexMapAnimType.None;
        public int StartFrame = 0;
        public int EndFrame = 11;
    }

    public class MexMapGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="MapSpaces"></param>
        /// <returns></returns>
        public static MEX_mexMapData GenerateMexMap(SBM_MnSelectStageDataTable stage)
        {
            return GenerateMexMap(stage, GenerateSpacesFromDefault(stage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="MapSpaces"></param>
        public static MEX_mexMapData GenerateMexMap(SBM_MnSelectStageDataTable stage, IEnumerable<MexMapSpace> MapSpaces)
        {
            MEX_mexMapData mapData = new MEX_mexMapData();

            var texanim = new HSD_TexAnim();
            HSD_JOBJ root = new HSD_JOBJ()
            {
                SX = 1, SY = 1, SZ = 1,
                Flags = JOBJ_FLAG.CLASSICAL_SCALING
            };
            HSD_AnimJoint animRoot = new HSD_AnimJoint();
            List<FOBJKey> Keys = new List<FOBJKey>();

            {
                var tobjs = stage.IconMaterialAnimation.Child.MaterialAnimation.Next.TextureAnimation.ToTOBJs();
                var index = texanim.AddImage(tobjs[0]);
                Keys.Add(new FOBJKey() { Frame = index, Value = index, InterpolationType = GXInterpolationType.HSD_A_OP_CON });
                index = texanim.AddImage(tobjs[1]);
                Keys.Add(new FOBJKey() { Frame = index, Value = index, InterpolationType = GXInterpolationType.HSD_A_OP_CON });
            }

            foreach (var v in MapSpaces)
            {
                var index = texanim.AddImage(v.TOBJ);
                if (index == -1)
                    index = 0;
                Keys.Add(new FOBJKey() { Frame = index, Value = index, InterpolationType = GXInterpolationType.HSD_A_OP_CON });
                v.JOBJ.Next = null;
                v.JOBJ.Child = null;
                root.AddChild(v.JOBJ);
                animRoot.AddChild(GenerateAnimJoint(v));
            }
            Keys.Add(new FOBJKey() { Frame = 1600, Value = 0, InterpolationType = GXInterpolationType.HSD_A_OP_CON });

            texanim.GXTexMapID = HSDRaw.GX.GXTexMapID.GX_TEXMAP0;
            texanim.AnimationObject = new HSD_AOBJ();
            texanim.AnimationObject.EndFrame = 1600;
            texanim.AnimationObject.FObjDesc = new HSD_FOBJDesc();
            texanim.AnimationObject.FObjDesc.SetKeys(Keys, (byte)TexTrackType.HSD_A_T_TIMG);
            texanim.AnimationObject.FObjDesc.Next = new HSD_FOBJDesc();
            texanim.AnimationObject.FObjDesc.Next.SetKeys(Keys, (byte)TexTrackType.HSD_A_T_TCLT);

            var iconJOBJ = HSDAccessor.DeepClone<HSD_JOBJ>(stage.Icon3Model);
            iconJOBJ.Child = iconJOBJ.Child.Next;
            var iconAnimJoint = HSDAccessor.DeepClone<HSD_AnimJoint>(stage.Icon3Animation);
            iconAnimJoint.Child = iconAnimJoint.Child.Next;
            var iconMatAnimJoint = HSDAccessor.DeepClone<HSD_MatAnimJoint>(stage.Icon3MaterialAnimation);
            iconMatAnimJoint.Child = iconMatAnimJoint.Child.Next;
            iconMatAnimJoint.Child.MaterialAnimation.Next.TextureAnimation = texanim;

            mapData.IconModel = iconJOBJ;
            mapData.IconAnimJoint = iconAnimJoint;
            mapData.IconMatAnimJoint = iconMatAnimJoint;
            mapData.PositionModel = root;
            mapData.PositionAnimJoint = animRoot;

            return mapData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="space"></param>
        /// <returns></returns>
        private static HSD_AnimJoint GenerateAnimJoint(MexMapSpace space)
        {
            HSD_AnimJoint joint = new HSD_AnimJoint();

            // starting frame-600
            // 1600 total frames
            if(space.AnimType == MexMapAnimType.SlideInFromRight)
                joint.AOBJ = GenerateAOBJ(36, space.JOBJ.TX, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_TRAX);
            if (space.AnimType == MexMapAnimType.SlideInFromLeft)
                joint.AOBJ = GenerateAOBJ(-40, space.JOBJ.TX, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_TRAX);
            if (space.AnimType == MexMapAnimType.SlideInFromTop)
                joint.AOBJ = GenerateAOBJ(36, space.JOBJ.TY, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_TRAY);
            if (space.AnimType == MexMapAnimType.SlideInFromBottom)
                joint.AOBJ = GenerateAOBJ(-40, space.JOBJ.TY, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_TRAY);
            if (space.AnimType == MexMapAnimType.GrowFromNothing)
            {
                joint.AOBJ = GenerateAOBJ(0, space.JOBJ.SX, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_SCAX);
                joint.AOBJ.FObjDesc.Next = GenerateAOBJ(0, space.JOBJ.SY, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_SCAY).FObjDesc;
            }
            if (space.AnimType == MexMapAnimType.SpinIn)
            {
                joint.AOBJ = GenerateAOBJ(0, space.JOBJ.SX, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_SCAX);
                joint.AOBJ.FObjDesc.Add(GenerateAOBJ(0, space.JOBJ.SY, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_SCAY).FObjDesc);
                joint.AOBJ.FObjDesc.Add(GenerateAOBJ(-4, 0, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_ROTZ).FObjDesc);
            }
            if (space.AnimType == MexMapAnimType.FlipIn)
            {
                joint.AOBJ = GenerateAOBJ(0, space.JOBJ.SX, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_SCAX);
                joint.AOBJ.FObjDesc.Add(GenerateAOBJ(0, space.JOBJ.SY, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_SCAY).FObjDesc);
                joint.AOBJ.FObjDesc.Add(GenerateAOBJ(-4, 0, space.StartFrame, space.EndFrame, JointTrackType.HSD_A_J_ROTY).FObjDesc);
            }

            return joint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endValue"></param>
        /// <param name="startFrame"></param>
        /// <param name="endFrame"></param>
        /// <param name="trackType"></param>
        /// <returns></returns>
        private static HSD_AOBJ GenerateAOBJ(float startValue, float endValue, float startFrame, float endFrame, JointTrackType trackType)
        {
            List<FOBJKey> keys = new List<FOBJKey>();
            if (startFrame != 0)
                keys.Add(new FOBJKey() { Frame = 0, Value = startValue, InterpolationType = GXInterpolationType.HSD_A_OP_CON });

            keys.Add(new FOBJKey() { Frame = startFrame, Value = startValue, InterpolationType = GXInterpolationType.HSD_A_OP_LIN });

            keys.Add(new FOBJKey() { Frame = endFrame, Value = endValue, InterpolationType = GXInterpolationType.HSD_A_OP_CON });
            keys.Add(new FOBJKey() { Frame = 1600, Value = endValue, InterpolationType = GXInterpolationType.HSD_A_OP_CON });

            var aobj = new HSD_AOBJ();
            aobj.EndFrame = 1600;
            aobj.Flags = AOBJ_Flags.FIRST_PLAY;
            aobj.FObjDesc = new HSD_FOBJDesc();
            aobj.FObjDesc.SetKeys(keys, (byte)trackType);

            return aobj;
        }

        private static Dictionary<int, int> unswizzle = new Dictionary<int, int>()
        {
            { 13, 11},
            { 14, 12},
            { 15, 13},
            { 16, 14},
            { 17, 15},

            { 18, 16},

            { 11, 17},
            { 12, 18},
        };

        private static Dictionary<int, int> texunswizzle = new Dictionary<int, int>()
        {
            { 0, 0},
            { 1, 1},
            { 2, 2},
            { 3, 4},
            { 4, 5},
            { 5, 6},
            { 6, 3},
            { 7, 9},
            { 8, 8},
            { 9, 7},
            { 10, 10},
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        private static List<MexMapSpace> GenerateSpacesFromDefault(SBM_MnSelectStageDataTable stage)
        {
            List<MexMapSpace> spaces = new List<MexMapSpace>();

            var tex0 = stage.Icon3MaterialAnimation.Child.Next.MaterialAnimation.Next.TextureAnimation.ToTOBJs();
            var tex0_extra = stage.Icon3MaterialAnimation.Child.MaterialAnimation.Next.TextureAnimation.ToTOBJs();
            var tex1 = stage.IconMaterialAnimation.Child.MaterialAnimation.Next.TextureAnimation.ToTOBJs();
            var tex2 = stage.Icon2MaterialAnimation.Child.MaterialAnimation.Next.TextureAnimation.ToTOBJs();

            var g1 = tex0.Length - 2;
            var g2 = tex0.Length - 2 + tex1.Length - 2;
            var g3 = tex0.Length - 2 + tex1.Length - 2 + tex2.Length - 2;

            for (int i = 0; i < stage.PositionModel.Children.Length; i++)
            {
                var childIndex = i;
                if (unswizzle.ContainsKey(i))
                    childIndex = unswizzle[i];

                HSD_TOBJ tobj = null;
                var anim = stage.PositionAnimation.Children[childIndex].AOBJ.FObjDesc.GetDecodedKeys();
                var Y = stage.PositionModel.Children[childIndex].TY;
                var SX = 1f;
                var SY = 1f;

                if (i >= g3)
                    tobj = null; // RandomIcon
                else
                if (i >= g2)
                {
                    tobj = tex2[i - g2 + 2];
                    SX = 0.8f;
                    SY = 0.8f;
                }
                else
                if (i >= g1)
                {
                    tobj = tex1[i - g1 + 2];
                    SY = 1.1f;
                }
                else
                {
                    tobj = tex0[texunswizzle[i] + 2];
                    spaces.Add(new MexMapSpace()
                    {
                        X = anim[anim.Count - 1].Value,
                        Y = Y,
                        TOBJ = tobj,
                        AnimType = MexMapAnimType.SlideInFromRight
                    });
                    Y -= 5.6f;
                    tobj = tex0_extra[texunswizzle[i] + 2];
                }

                spaces.Add(new MexMapSpace()
                {
                    X = anim[anim.Count - 1].Value,
                    Y = Y,
                    SX = SX,
                    SY = SY,
                    TOBJ = tobj,
                    AnimType = MexMapAnimType.SlideInFromRight
                });
            }
            return spaces;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<MexMapSpace> LoadMexMapDataFromSymbol(MEX_mexMapData data)
        {
            List<MexMapSpace> spaces = new List<MexMapSpace>();

            var tobjs = data.IconMatAnimJoint.Child.MaterialAnimation.Next.TextureAnimation.ToTOBJs();
            var tobjanim = data.IconMatAnimJoint.Child.MaterialAnimation.Next.TextureAnimation.AnimationObject.FObjDesc.GetDecodedKeys();

            for (int i = 0; i < data.PositionModel.Children.Length; i++)
            {
                var tobj = tobjs[(int)tobjanim[i + 2].Value];
                var jobj = data.PositionModel.Children[i];
                var aobj = data.PositionAnimJoint.Children[i].AOBJ;

                MexMapSpace space = new MexMapSpace();
                space.TOBJ = tobj;
                space.JOBJ = jobj;

                // assume anim type
                if(aobj != null && aobj.FObjDesc != null)
                {
                    var fobjs = aobj.FObjDesc.List;

                    var hasScaX = fobjs.Any(e => e.JointTrackType == JointTrackType.HSD_A_J_SCAX);
                    var hasScaY = fobjs.Any(e => e.JointTrackType == JointTrackType.HSD_A_J_SCAY);
                    var hasTraX = fobjs.Any(e => e.JointTrackType == JointTrackType.HSD_A_J_TRAX);
                    var hasTraY = fobjs.Any(e => e.JointTrackType == JointTrackType.HSD_A_J_TRAY);
                    var hasRotY = fobjs.Any(e => e.JointTrackType == JointTrackType.HSD_A_J_ROTY);
                    var hasRotZ = fobjs.Any(e => e.JointTrackType == JointTrackType.HSD_A_J_ROTZ);

                    var keys = fobjs[0].GetDecodedKeys();
                    if (keys.Count >= 3)
                    {
                        var startValue = keys[0].Value;
                        var endValue = keys[keys.Count - 1].Value;
                        space.EndFrame = (int)keys[keys.Count - 2].Frame;
                        space.StartFrame = (int)keys[keys.Count - 3].Frame;

                        if (hasScaX && hasScaY)
                        {
                            if (hasRotZ)
                                space.AnimType = MexMapAnimType.SpinIn;
                            else
                            if (hasRotY)
                                space.AnimType = MexMapAnimType.FlipIn;
                            else
                                space.AnimType = MexMapAnimType.GrowFromNothing;
                        }
                        else if (hasTraX)
                        {
                            if (startValue < endValue)
                                space.AnimType = MexMapAnimType.SlideInFromLeft;
                            else
                                space.AnimType = MexMapAnimType.SlideInFromRight;
                        }
                        else if (hasTraY)
                        {
                            if (startValue < endValue)
                                space.AnimType = MexMapAnimType.SlideInFromBottom;
                            else
                                space.AnimType = MexMapAnimType.SlideInFromTop;
                        }
                    }
                }

                spaces.Add(space);
            }

            return spaces;
        }
    }
}