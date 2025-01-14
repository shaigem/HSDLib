﻿using HSDRaw.Common.Animation;
using System;
using System.ComponentModel;
using System.Text;

namespace HSDRaw.Common
{
    [Flags]
    public enum JOBJ_FLAG
    {
        SKELETON = (1 << 0),
        SKELETON_ROOT = (1 << 1),
        ENVELOPE_MODEL = (1 << 2),
        CLASSICAL_SCALING = (1 << 3),
        HIDDEN = (1 << 4),
        PTCL = (1 << 5),
        MTX_DIRTY = (1 << 6),
        LIGHTING = (1 << 7),
        TEXGEN = (1 << 8),
        BILLBOARD = (1 << 9),
        VBILLBOARD = (2 << 9),
        HBILLBOARD = (3 << 9),
        RBILLBOARD = (4 << 9),
        INSTANCE = (1 << 12),
        PBILLBOARD = (1 << 13),
        SPLINE = (1 << 14),
        FLIP_IK = (1 << 15),
        SPECULAR = (1 << 16),
        USE_QUATERNION = (1 << 17),
        OPA = (1 << 18),
        XLU = (1 << 19),
        TEXEDGE = (1 << 20),
        NULL = (0 << 21),
        JOINT1 = (1 << 21),
        JOINT2 = (2 << 21),
        EFFECTOR = (3 << 21),
        USER_DEFINED_MTX = (1 << 23),
        MTX_INDEPEND_PARENT = (1 << 24),
        MTX_INDEPEND_SRT = (1 << 25),
        ROOT_OPA = (1 << 28),
        ROOT_XLU = (1 << 29),
        ROOT_TEXEDGE = (1 << 30),

        // custom
        MTX_SCALE_COMPENSATE = (1 << 26),
    }

    public class HSD_JOBJ : HSDTreeAccessor<HSD_JOBJ>
    {
        public override int TrimmedSize { get; } = 0x40;

        /// <summary>
        /// Used for class lookup, but you can put whatever you want here
        /// </summary>
        public string ClassName
        {
            get => _s.GetString(0x00);
            set => _s.SetString(0x00, value);
        }

        public JOBJ_FLAG Flags 
        { 
            get => (JOBJ_FLAG)_s.GetInt32(0x04); 
            set => _s.SetInt32(0x04, (int)value);
        }

        public override HSD_JOBJ Child { get => _s.GetReference<HSD_JOBJ>(0x08); set => _s.SetReference(0x08, value); }

        public override HSD_JOBJ Next { get => _s.GetReference<HSD_JOBJ>(0x0C); set => _s.SetReference(0x0C, value); }
        
        public HSD_DOBJ Dobj { get => !Flags.HasFlag(JOBJ_FLAG.SPLINE) && !Flags.HasFlag(JOBJ_FLAG.PTCL) ? _s.GetReference<HSD_DOBJ>(0x10) : null; set { _s.SetReference(0x10, value); Flags &= ~JOBJ_FLAG.SPLINE; Flags &= ~JOBJ_FLAG.PTCL; } }

        
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public HSD_Spline Spline { get => Flags.HasFlag(JOBJ_FLAG.SPLINE) ? _s.GetReference<HSD_Spline>(0x10) : null; set { _s.SetReference(0x10, value); Flags |= JOBJ_FLAG.SPLINE; } }

        
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public HSD_ParticleJoint ParticleJoint { get => Flags.HasFlag(JOBJ_FLAG.PTCL) ? _s.GetReference<HSD_ParticleJoint>(0x10) : null; set { _s.SetReference(0x10, value); Flags |= JOBJ_FLAG.PTCL; } }

        public float RX { get => _s.GetFloat(0x14); set => _s.SetFloat(0x14, value); }
        public float RY { get => _s.GetFloat(0x18); set => _s.SetFloat(0x18, value); }
        public float RZ { get => _s.GetFloat(0x1C); set => _s.SetFloat(0x1C, value); }
        public float SX { get => _s.GetFloat(0x20); set => _s.SetFloat(0x20, value); }
        public float SY { get => _s.GetFloat(0x24); set => _s.SetFloat(0x24, value); }
        public float SZ { get => _s.GetFloat(0x28); set => _s.SetFloat(0x28, value); }
        public float TX { get => _s.GetFloat(0x2C); set => _s.SetFloat(0x2C, value); }
        public float TY { get => _s.GetFloat(0x30); set => _s.SetFloat(0x30, value); }
        public float TZ { get => _s.GetFloat(0x34); set => _s.SetFloat(0x34, value); }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public HSD_Matrix4x3 InverseWorldTransform { get => _s.GetReference<HSD_Matrix4x3>(0x38); set => _s.SetReference(0x38, value); }

        public HSD_ROBJ ROBJ { get => _s.GetReference<HSD_ROBJ>(0x3C); set => _s.SetReference(0x3C, value); }

        protected override int Trim()
        {
            // quit optimizing these away
            _s.CanBeBuffer = false;

            return base.Trim();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public float GetDefaultValue(JointTrackType type)
        {
            switch (type)
            {
                case JointTrackType.HSD_A_J_TRAX: return TX;
                case JointTrackType.HSD_A_J_TRAY: return TY;
                case JointTrackType.HSD_A_J_TRAZ: return TZ;
                case JointTrackType.HSD_A_J_ROTX: return RX;
                case JointTrackType.HSD_A_J_ROTY: return RY;
                case JointTrackType.HSD_A_J_ROTZ: return RZ;
                case JointTrackType.HSD_A_J_SCAX: return SX;
                case JointTrackType.HSD_A_J_SCAY: return SY;
                case JointTrackType.HSD_A_J_SCAZ: return SZ;
            }
            return 0;
        }

        /// <summary>
        /// Autometically sets needed flags for self and all children
        /// </summary>
        public void UpdateFlags()
        {
            var list = BreathFirstList;
            list.Reverse();

            foreach (var j in list)
            {
                if (j.Dobj != null)
                {
                    bool xlu = false;
                    bool opa = false;

                    foreach (var dobj in j.Dobj.List)
                    {
                        if (dobj.Mobj != null && dobj.Mobj.RenderFlags.HasFlag(RENDER_MODE.XLU))
                        {
                            j.Flags |= JOBJ_FLAG.XLU;
                            j.Flags |= JOBJ_FLAG.TEXEDGE;
                            xlu = true;
                        }
                        else
                        {
                            j.Flags &= ~JOBJ_FLAG.XLU;
                            j.Flags &= ~JOBJ_FLAG.TEXEDGE;
                            opa = true;
                        }

                        if (dobj.Mobj != null && dobj.Mobj.RenderFlags.HasFlag(RENDER_MODE.DIFFUSE))
                            j.Flags |= JOBJ_FLAG.LIGHTING;
                        else
                            j.Flags &= ~JOBJ_FLAG.LIGHTING;

                        if (dobj.Mobj != null && dobj.Mobj.RenderFlags.HasFlag(RENDER_MODE.SPECULAR))
                            j.Flags |= JOBJ_FLAG.SPECULAR;
                        else
                            j.Flags &= ~JOBJ_FLAG.SPECULAR;

                        if (dobj.Pobj != null)
                        {
                            j.Flags &= ~JOBJ_FLAG.ENVELOPE_MODEL;
                            foreach (var pobj in dobj.Pobj.List)
                            {
                                if (pobj.Flags.HasFlag(POBJ_FLAG.ENVELOPE))
                                    j.Flags |= JOBJ_FLAG.ENVELOPE_MODEL;
                            }
                        }
                    }

                    if (opa)
                        j.Flags |= JOBJ_FLAG.OPA;
                    else
                        j.Flags &= ~JOBJ_FLAG.OPA;

                    if (xlu)
                        j.Flags |= JOBJ_FLAG.XLU | JOBJ_FLAG.TEXEDGE;
                    else
                        j.Flags &= ~JOBJ_FLAG.XLU;
                }

                if (j.InverseWorldTransform != null)
                    j.Flags |= JOBJ_FLAG.SKELETON;
                else
                    j.Flags &= ~JOBJ_FLAG.SKELETON;

                if (ChildHasFlag(j.Child, JOBJ_FLAG.XLU))
                    j.Flags |= JOBJ_FLAG.ROOT_XLU;
                else
                    j.Flags &= ~JOBJ_FLAG.ROOT_XLU;

                if (ChildHasFlag(j.Child, JOBJ_FLAG.OPA))
                    j.Flags |= JOBJ_FLAG.ROOT_OPA;
                else
                    j.Flags &= ~JOBJ_FLAG.ROOT_OPA;

                if (ChildHasFlag(j.Child, JOBJ_FLAG.TEXEDGE))
                    j.Flags |= JOBJ_FLAG.ROOT_TEXEDGE;
                else
                    j.Flags &= ~JOBJ_FLAG.ROOT_TEXEDGE;
            }

            if (ChildHasFlag(Child, JOBJ_FLAG.SKELETON))
                Flags |= JOBJ_FLAG.SKELETON_ROOT;
            else
                Flags &= ~JOBJ_FLAG.SKELETON_ROOT;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobj"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private static bool ChildHasFlag(HSD_JOBJ jobj, JOBJ_FLAG flag)
        {
            if (jobj == null)
                return false;

            bool hasFlag = jobj.Flags.HasFlag(flag);

            foreach (var c in jobj.Children)
            {
                if (ChildHasFlag(c, flag))
                    hasFlag = true;
            }

            if (jobj.Next != null)
            {
                if (ChildHasFlag(jobj.Next, flag))
                    hasFlag = true;
            }

            return hasFlag;
        }
    }
}
