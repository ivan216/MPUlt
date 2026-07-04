using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections;
using System.IO;

namespace _3dedit {
    class PBaseAxis {
        internal int Id;
        internal double[] Dir;
        internal int NLayers;   // number of rotational layers (for GUI).
        internal double[] Cut;   // in increasing order, zero is duplicated
        internal int FixedMask;
        internal int[][] Layers;
        internal PBaseTwist[] Twists;
        internal int NPrimaryTwists;
        internal List<double[]> SMatrices;  // group matrices that preserve this axis

        internal PBaseAxis(double[] dir) {
            Dir=dir;
            FixedMask=0;
            SMatrices = new List<double[]>();
        }


        internal int Remask(int mask) {
            return mask&~FixedMask;
        }

        internal int ReverseMask(int ms) {
            int r=0;
            for(int i=0;i<NLayers;i++) {
                r=(r<<1)|(ms&1);
                ms>>=1;
            }
            return r;
        }

        static int CmpDouble(double x,double y) {
            return x<y ? 1 : x==y ? 0 : -1;
        }

        internal void AdjustCuts() {
            Array.Sort<double>(Cut,CmpDouble);
            NLayers=Cut.Length+1;
            FixedMask=0;
        }

        internal void AddSMatrix(double[] G) {
            SMatrices.Add(G);
        }

        internal void ExpandPrimaryTwists() {
            int dim = Dir.Length;
            NPrimaryTwists = Twists.Length;
            List<PBaseTwist> list = new List<PBaseTwist>();
            // Step 1: add identity-conjugated originals (preserve primary order)
            for(int ti = 0; ti < NPrimaryTwists; ti++)
                list.Add(new PBaseTwist(Twists[ti], SMatrices[0], Dir));
            // Step 2: add unique SMatrices conjugates of each primary twist
            for(int ti = 0; ti < NPrimaryTwists; ti++) {
                for(int si = 1; si < SMatrices.Count; si++) {
                    PBaseTwist ntw = new PBaseTwist(Twists[ti], SMatrices[si], Dir);
                    bool found = false;
                    foreach(PBaseTwist t in list) {
                        bool qr;
                        if(PGeom.MatrixEqual(ntw.Matr, t.Matr, out qr, dim)) {
                            found = true;
                            break;
                        }
                    }
                    if(!found) list.Add(ntw);
                }
            }
            Twists = list.ToArray();
        }

        internal void DebugPrint(TextWriter tw) {
            tw.WriteLine("Base Axis {0}",Id);
            tw.Write("  Dir:");
            PuzzleStructure.PrintVec(Dir,tw,false);
            tw.WriteLine();
            tw.Write("  NLayers: {0}",NLayers);
            tw.Write("  FixedMask: {0}",FixedMask);
            tw.Write("  Cut:");
            PuzzleStructure.PrintVec(Cut,tw,false);
            tw.WriteLine();
            /*
            for(int i=0;i<NLayers;i++){
                if(Layers[i]!=null){
                    tw.Write("  Layer {0}:",i);
                    PuzzleStructure.PrintIArr(Layers[i],tw);
                    tw.WriteLine();
                }
            }*/
            tw.WriteLine("  Twists: {0}",Twists.Length);
            /*
            foreach(PBaseTwist tww in Twists){
                tww.DebugPrint(tw);
            }*/
        }
        internal int GetRank(int lv) {
            if(lv>=NLayers-1 || Cut[lv]<0) {
                for(int i=lv;--i>=0;) if(Cut[i]>=0) return lv-1-i;
                return lv;
            } else if(Cut[lv]==0) return 0;
            else {
                for(int i=lv+1;i<NLayers-1;i++) if(Cut[i]<=0) return i-lv;
                return NLayers-1-lv;
            }
        }

        internal void GenerateTwists() {
            throw new NotImplementedException();
        }

        internal int FindTwist4D(double[] pt, int type) {
            int dim = Dir.Length;
            double[] proj = new double[dim];
            double l1 = PGeom.DotProd(pt, Dir) / PGeom.DotProd(Dir, Dir);
            for(int i = 0; i < dim; i++) proj[i] = pt[i] - l1 * Dir[i];
            double rad = PGeom.VLength(proj);
            double bestDist = double.MaxValue;
            int bestTw = 0;
            double altDist = double.MaxValue;
            int altTw = 0;
            for(int j = 0; j < Twists.Length; j++) {
                PBaseTwist tw = Twists[j];
                if(tw.NTwist != 0) {
                    l1 = -PGeom.DotProd(tw.Pole, proj) / rad;
                    double plen = PGeom.VLength(tw.Pole);
                    double dist = Math.Sqrt(plen * plen - l1 * l1) / Math.Sqrt(plen);
                    if(tw.NTwist == type) {
                        if(dist < bestDist) {
                            bestTw = (l1 < 0.0) ? (-j - 1) : (j + 1);
                            bestDist = dist;
                        }
                    } else if(dist < altDist) {
                        altTw = (l1 < 0.0) ? (-j - 1) : (j + 1);
                        altDist = dist;
                    }
                }
            }
            if(bestTw == 0 || (altTw != 0 && altDist * 1.1 < bestDist))
                return altTw;
            if(bestTw != 0)
                return bestTw;
            return 1;
        }
    }
    class PBaseTwist {
        internal double[] Dir;  // 2*dim (backward compat — twist vector)
        internal int Order;
        internal int[][] Map;   // sorting of stickers in layers of base axis
        internal int[][] InvMap;

        // New fields for N-segment twist support
        internal int Dim;
        internal double[] Orig;   // original twist vector (variable length)
        internal double[] Matr;   // flat matrix form (dim*dim)
        internal int NTwist;      // number of segments: 2=standard, 1=odd count (pure reflection), 0=even count≠2
        internal double MaxAngle;
        internal double[] Pole;   // 4D twist pole

        internal PBaseTwist(double[] dir) {
            Dir=dir;
            Order=PGeom.GetOrder(Dir);
            // Set defaults for backward compat
            Dim = dir.Length / 2;
            Orig = dir;
            Matr = PGeom.CreateMatrixFromTwist(dir, Dim);
            NTwist = 2;
            MaxAngle = 1.5707963267948966;
            Pole = null;
        }

        internal PBaseTwist(double[] twist, double[] axis) {
            Init(twist, axis);
        }

        internal PBaseTwist(PBaseTwist tw, double[] matr, double[] axis) {
            double[] t = PGeom.ApplyMatrix(matr, tw.Orig, axis.Length);
            Init(t, axis);
        }

        private void Init(double[] twist, double[] axis) {
            Dim = axis.Length;
            Dir = twist;
            Orig = twist;
            Matr = PGeom.CreateMatrixFromTwist(twist, Dim);
            Order = PGeom.GetOrder(twist, Dim);
            NTwist = twist.Length / Dim;
            if(NTwist != 2) NTwist %= 2;
            // MaxAngle: approximate for fractional twists
            int num = twist.Length / Dim;
            MaxAngle = (1 + num % 2 == 1) ? 0.0 : 1.5707963267948966;
            int pairs = num / 2;
            for(int i = 0; i < pairs; i++) {
                double ang = PGeom.Angle(twist, i * 2 * Dim, Dim);
                if(ang > MaxAngle) MaxAngle = ang;
            }
            if(Dim == 4) Pole = PGeom.GetTwistPole(twist, axis);
            else Pole = null;
        }
        internal int ReAngle(int angle) {
            angle%=Order;
            if(angle<0) angle+=Order;
            return angle;
        }

        internal int NormAngle(int angle) {
            angle%=Order;
            if(angle<0) angle+=Order;
            if(angle>Order/2) angle-=Order;
            return angle;
        }


        internal void DebugPrint(TextWriter tw) {
            tw.Write("  Twist");
            PuzzleStructure.PrintVec(Dir,tw,true);
            tw.WriteLine("    Order={0}",Order);
            tw.WriteLine("    Map:");
            for(int i=0;i<Map.Length;i++) {
                if(Map[i]!=null) {
                    tw.Write("      Level {0}:",i);
                    PuzzleStructure.PrintIArr(Map[i],tw);
                    tw.WriteLine();
                }
            }
            tw.WriteLine("    InvMap:");
            for(int i=0;i<InvMap.Length;i++) {
                if(InvMap[i]!=null) {
                    tw.Write("      Level {0}:",i);
                    PuzzleStructure.PrintIArr(InvMap[i],tw);
                    tw.WriteLine();
                }
            }
        }
    }

    class PBaseFace {
        internal int Id;
        internal double[] Pole;
        internal double[][] FPoles;
        internal int NCutAxes;
        internal int[] CutAxes;
        internal int[] AxisLayers;  // for all axes; -1 => from CutAxes
        internal int NStickers;
        internal byte[,] StickerMask; // NStickers,NCutAxes
        internal PMesh[] StickerMesh;
        internal PMesh FaceMesh;
        internal List<double[]> SMatrices;

        internal PBaseFace(double[] pole) {
            Pole=pole;
            SMatrices = new List<double[]>();
            SMatrices.Add(PGeom.CreateMatrixIdent(pole.Length));
        }

        internal void SetStickers(LMesh M,CutNetwork CN,List<PAxis> Axes,double[][]fctrs) {
            int dim=Pole.Length;
            NCutAxes=0;
            int nax=Axes.Count;
            for(int i=0;i<nax;i++) if(AxisLayers[i]<0) NCutAxes++;
            CutAxes=new int[NCutAxes];
            int d=0;
            int rnk=0;
            for(int u=0;u<nax;u++) {
                if(AxisLayers[u]<0) CutAxes[d++]=u;
                else rnk+=Axes[u].Base.GetRank(AxisLayers[u]);
            }

            NStickers=CN.Nodes.Length;
            StickerMask=new byte[NStickers,NCutAxes];
            StickerMesh=new PMesh[NStickers];
            int nstk=0;
            for(int i=0;i<NStickers;i++) {
                PMesh xx=CN.GetPMesh(i);
                if(fctrs!=null) {
                    bool qg=false;
                    foreach(double[] p in fctrs) {
                        if(PGeom.VertEqual(p,xx.Ctr)) {
                            qg=true;
                            break;
                        }
                    }
                    if(!qg) continue;
                }

                xx.FCtr=Pole;
                double[] ctr=xx.GetMCtr();
                int rnk1=rnk;
                for(int j=0;j<NCutAxes;j++) {
                    PAxis ax=Axes[CutAxes[j]];
                    double[] h=ax.Dir;
                    double lh=PGeom.DotProd(h,h);
                    double s=0;
                    for(int k=0;k<dim;k++) s+=ctr[k]*h[k];
                    s/=lh;
                    int lv=ax.Base.NLayers-1;
                    for(int g=0;g<lv;g++) {
                        if(s>ax.Base.Cut[g]) { lv=g; break; }
                    }
                    rnk1+=ax.Base.GetRank(lv);
                    StickerMask[nstk,j]=(byte)lv;
                }
                xx.Rank=rnk1;
                StickerMesh[nstk++]=xx;
            }
            if(nstk!=NStickers) {
                PMesh[] stkm=new PMesh[nstk];
                byte[,] stkmsk=new byte[nstk,NCutAxes];
                for(int i=0;i<nstk;i++) {
                    stkm[i]=StickerMesh[i];
                    for(int j=0;j<NCutAxes;j++) stkmsk[i,j]=StickerMask[i,j];
                }
                StickerMask=stkmsk;
                StickerMesh=stkm;
                NStickers=nstk;
            }
        }

        internal int MinRank() {
            int res=int.MaxValue;
            foreach(PMesh p in StickerMesh) res=Math.Min(res,p.Rank);
            return res;
        }
        internal void SubRank(int r) {
            foreach(PMesh p in StickerMesh) p.Rank-=r;
        }
        internal int FindByMask(int[] StkMask) {
            for(int i=0;i<NStickers;i++) {
                for(int j=0;j<NCutAxes;j++) {
                    if(StickerMask[i,j]!=StkMask[j]) goto _1;
                }
                return i;
_1: ;
            }
            throw new Exception("Can't find sticker by mask");
        }

        internal void DebugPrint(TextWriter tw) {
            tw.WriteLine("Base Face {0}:",Id);
            tw.Write("  Pole:");
            PuzzleStructure.PrintVec(Pole,tw,false);
            tw.WriteLine();
            tw.WriteLine("  NCutAxes: {0}",NCutAxes);
            tw.Write("  CutAxes:");
            PuzzleStructure.PrintIArr(CutAxes,tw);
            tw.WriteLine();
            /*
            tw.Write("  AxesLayers:");
            PuzzleStructure.PrintIArr(AxisLayers,tw);
            tw.WriteLine();*/
            tw.WriteLine("  NStickers: {0}",NStickers);
            for(int i=0;i<NStickers;i++){
                tw.Write("Sticker {0}: NV={1}, NE={2}, NF={3}, Ctr=",
                    i,StickerMesh[i].NV,StickerMesh[i].NE,StickerMesh[i].NF);
                PuzzleStructure.PrintVec(StickerMesh[i].Ctr,tw,false);
                tw.Write(" Mask:");
                for(int j=0;j<NCutAxes;j++) tw.Write(" {0}",StickerMask[i,j]);
                tw.WriteLine();
            }
        }

        internal void AddSMatrix(double[] M) {
            int dim = (int)Math.Sqrt(M.Length);
            foreach(double[] m in SMatrices) {
                bool qr;
                if(PGeom.MatrixEqual(M, m, out qr, dim)) return;
            }
            SMatrices.Add(M);
        }

        internal void CloseSMatrixSet() {
            // No longer needed — Group closure is done in CloseGroup()
        }
    }

    class PAxis {
        internal int Id;
        internal PBaseAxis Base;
        internal double[,] Matrix;
        internal double[] Dir;
        internal int[][] Layers; // actual stickers by layers
        internal double[][] Twists;

        internal PAxis(PBaseAxis bas) {
            Base=bas;
            Dir=bas.Dir;
            int dim=Dir.Length;
            Matrix=PGeom.MatrixIdentity(dim);
            int ntw=bas.Twists.Length;
            Twists=new double[ntw][];
            for(int i=0;i<ntw;i++) Twists[i]=bas.Twists[i].Dir;
        }

        internal PAxis(PAxis src,double []tw) {
            Base=src.Base;
            int dim = src.Dir.Length;
            Dir = PGeom.ApplyTwistN(tw, src.Dir, dim);
            Matrix = PGeom.ApplyTwist(tw, src.Matrix);
            int ntw=src.Twists.Length;
            Twists=new double[ntw][];
            for(int i=0;i<ntw;i++) Twists[i]=PGeom.ApplyTwistN(tw, src.Twists[i], dim);
        }

        internal PAxis(PBaseAxis bas, double[] matr, int id) {
            Base = bas;
            Id = id;
            int dim = bas.Dir.Length;
            // Convert flat matrix to 2D for backward compat
            Matrix = new double[dim, dim];
            for(int i = 0; i < dim; i++)
                for(int j = 0; j < dim; j++)
                    Matrix[i, j] = matr[i * dim + j];
            Dir = PGeom.ApplyMatrix(matr, bas.Dir, dim);
            int ntw = bas.Twists.Length;
            Twists = new double[ntw][];
            for(int i = 0; i < ntw; i++)
                Twists[i] = PGeom.ApplyMatrixToTwist(Matrix, bas.Twists[i].Dir);
        }

        internal int FindTwist(double[] p,double[,] matr,out bool qrev) {
            int dim = Base.Dir.Length;
            double[] q = PGeom.ApplyMatrixToTwist(matr, p);
            qrev=false;
            double[] m = PGeom.CreateMatrixFromTwist(q, dim);
            for(int i=0;i<Twists.Length;i++) {
                double[] mi = PGeom.CreateMatrixFromTwist(Twists[i], dim);
                if(PGeom.MatrixEqual(m, mi, out qrev, dim)) return i;
            }
            throw new Exception("Can't find twist");
        }

        internal int FindTwist(PAxis src, int tw, double[] matr, out bool qrev) {
            int dim = Base.Dir.Length;
            qrev = false;
            double[] t = src.Base.Twists[tw].Orig;
            // Convert 2D matrices to flat for use with flat ApplyMatrix
            double[] srcFlat = new double[dim * dim];
            double[] thisFlat = new double[dim * dim];
            for(int ii = 0; ii < dim; ii++)
                for(int jj = 0; jj < dim; jj++) {
                    srcFlat[ii * dim + jj] = src.Matrix[ii, jj];
                    thisFlat[ii * dim + jj] = this.Matrix[ii, jj];
                }
            t = PGeom.ApplyMatrix(srcFlat, t, dim);
            t = PGeom.ApplyMatrix(matr, t, dim);
            t = PGeom.ApplyInvMatrix(thisFlat, t, dim);
            double[] m = PGeom.CreateMatrixFromTwist(t, dim);
            for(int i = 0; i < this.Base.Twists.Length; i++) {
                if(PGeom.MatrixEqual(m, this.Base.Twists[i].Matr, out qrev, dim)) return i;
            }
            throw new Exception("Can't find twist");
        }

        internal void DebugPrint(TextWriter tw) {
            tw.WriteLine("Axis {0}:",Id);
            tw.WriteLine("  Base: {0}",Base.Id);
            tw.Write("  Dir:");
            PuzzleStructure.PrintVec(Dir,tw,false);
            tw.WriteLine();
/*
            for(int i=0;i<Layers.Length;i++){
                if(Layers[i]!=null){
                    tw.Write("  Layer {0}:",i);
                    PuzzleStructure.PrintIArr(Layers[i],tw);
                    tw.WriteLine();
                }
            }
            
            for(int i=0;i<Twists.Length;i++){
                tw.Write("  Twist {0}:",i);
                PuzzleStructure.PrintVec(Twists[i],tw,true);
                tw.WriteLine();                
            }*/
        }

    }
    class PFace {
        internal int Id;
        internal PBaseFace Base;
        internal double[,] Matrix;
        internal double[] Pole;

        internal int[] CutAxes;
        internal int FirstSticker;
        internal int RefAxis;

        internal PFace(PBaseFace bas) {
            Base=bas;
            Pole=bas.Pole;
            int dim=Pole.Length;
            Matrix=PGeom.MatrixIdentity(dim);
            RefAxis=0;
        }
        internal PFace(PFace src,double[] tw) {
            Base=src.Base;
            int dim = src.Pole.Length;
            Pole = PGeom.ApplyTwistN(tw, src.Pole, dim);
            Matrix = PGeom.ApplyTwist(tw, src.Matrix);
        }
        internal PFace(PBaseFace bas, double[] matr, int id) {
            Base = bas;
            Id = id;
            Pole = PGeom.ApplyMatrix(matr, bas.Pole, bas.Pole.Length);
            int dim = bas.Pole.Length;
            Matrix = new double[dim, dim];
            for(int i = 0; i < dim; i++)
                for(int j = 0; j < dim; j++)
                    Matrix[i, j] = matr[i * dim + j];
            RefAxis = 0;
        }

        internal void DebugPrint(TextWriter tw) {
            tw.WriteLine("Face {0}:",Id);
            tw.WriteLine("  Base: {0}",Base.Id);
            tw.Write("  Pole:");
            PuzzleStructure.PrintVec(Pole,tw,false);
            tw.WriteLine();
            tw.Write("  CutAxes:");
            PuzzleStructure.PrintIArr(CutAxes,tw);
            tw.WriteLine();
            tw.WriteLine("  First Sticker: {0}",FirstSticker);
        }
    }
    class PMesh {
        internal int PDim;
        internal int MinBDim;
        internal int Rank;
        internal int NV,NE,NF;
        internal double[] Ctr,FCtr;
        internal double[] Coords;
        internal int[] Edges;  // 2*NE
        internal int[] Faces;  // 3*NF

        internal PMesh(LMesh m) {
            Coords=m.pts;
            Edges=m.edges;
            Faces=m.faces;
            NV=m.npts;
            NE=m.nedges;
            NF=m.nfaces;
            PDim=m.pdim;
            Ctr=m.ctr;
            MinBDim=m.m_minBDim;
        }
        internal double[] GetMCtr() {
            double[] res=new double[PDim];
            for(int i=0;i<NV*PDim;i++) res[i%PDim]+=Coords[i]/NV;
            return res;         
        }
    }

    class PGeom {
        internal static int gcd(int a, int b) {
            while(b != 0) { int t = a % b; a = b; b = t; }
            return Math.Abs(a);
        }

        internal static int GetOrder(double[] tw) {
            return GetOrder(tw, tw.Length / 2);
        }

        internal static int GetOrder(double[] tw, int dim) {
            double _;
            return GetOrder(tw, dim, out _);
        }

        internal static int GetOrder(double[] tw, int dim, out double maxAng) {
            int num = tw.Length / dim;       // total segments
            int order = 1 + num % 2;          // 2 if odd (unpaired mirror), 1 if even
            maxAng = (order == 2) ? 1.5707963267948966 : 0.0;
            int pairs = num / 2;              // number of rotation-reflection pairs
            for(int i = 0; i < pairs; i++) {
                int idx = i * 2 * dim;
                double la = 0, lb = 0, pr = 0;
                for(int j = 0; j < dim; j++) {
                    la += tw[idx + j] * tw[idx + j];
                    lb += tw[idx + j + dim] * tw[idx + j + dim];
                    pr += tw[idx + j] * tw[idx + j + dim];
                }
                double a = Math.Acos(Math.Min(1, pr / Math.Sqrt(la * lb)));
                if(a > maxAng) maxAng = a;
                if(a != 0) a = Math.PI / a;
                int r = (int)Math.Round(a);
                if(r == 0 || Math.Abs(a - r) > 1e-4) throw new Exception("Wrong twist order " + a);
                int g = gcd(r, order);
                order = order * r / g;
            }
            return order;
        }

        internal static double[] ApplyTwist(double[] mov,double[] vec) {
            return ApplyTwistN(mov, vec, mov.Length / 2);
        }

        internal static double[] ApplyTwistN(double[] mov, double[] vec, int dim) {
            double[] res = (double[])vec.Clone();
            for(int k = 0; k < res.Length; k += dim) {
                for(int s = 0; s < mov.Length; s += dim) {
                    double sa = 0, sb = 0;
                    for(int i = 0; i < dim; i++) {
                        sa += mov[s + i] * mov[s + i];
                        sb += mov[s + i] * res[k + i];
                    }
                    double scale = 2 * sb / sa;
                    for(int i = 0; i < dim; i++) res[k + i] -= mov[s + i] * scale;
                }
            }
            return res;
        }


        internal static bool TwistsEqual(double[] v,double[] w,out bool qr) {
            qr=true;
            bool qp=true,qm=true;
            double vl=VLength2(v);
            double wl=VLength2(w);

            int d=v.Length/2;
            for(int i=1;i<d;i++) {
                for(int j=0;j<i;j++) {
                    double p=(v[i]*v[j+d]-v[j]*v[i+d])/vl;
                    double q=(w[i]*w[j+d]-w[j]*w[i+d])/wl;
                    if(qp && Math.Abs(p-q)>1e-3) qp=false;
                    if(qm && Math.Abs(p+q)>1e-3) qm=false;
                    if(!qp && !qm) return false;
                }
            }
            qr=qm;
            return true;
        }
        internal static double VLength(double[] v) {
            return VLength(v, 0, v.Length);
        }

        internal static double VLength(double[] v, int start, int len) {
            double r=0;
            for(int i=0;i<len;i++) r+=v[start+i]*v[start+i];
            return Math.Sqrt(r);
        }
        static double VLength2(double[] v) {
            double r=0,r1=0;
            int l=v.Length/2;
            for(int i=0;i<l;i++) r+=v[i]*v[i];
            for(int i=l;i<2*l;i++) r1+=v[i]*v[i];
            return Math.Sqrt(r*r1);
        }


        internal static double[,] MatrixIdentity(int dim) {
            double[,] M=new double[dim,dim];
            for(int i=0;i<dim;i++) M[i,i]=1;
            return M;
        }

        internal static bool VertEqual(double[] v,double[] p) {
            int dim=v.Length;
            for(int i=0;i<dim;i++) if(Math.Abs(v[i]-p[i])>0.001) return false;
            return true;
        }

        internal static double[,] ApplyTwist(double[] tw,double[,] matr) {
            int dim=matr.GetLength(0);
            double[,] res=new double[dim,dim];
            double[] m=new double[dim];
            for(int i=0;i<dim;i++) {
                for(int j=0;j<dim;j++) m[j]=matr[i,j];
                double[] mm=ApplyTwistN(tw, m, dim);
                for(int j=0;j<dim;j++) res[i,j]=mm[j];
            }
            return res;
        }

        internal static bool AxisEqual(double[] v,double[] p,out bool qr) {
            bool qp=true,qm=true;
            int dim=v.Length;
            for(int i=0;(qp||qm)&&i<dim;i++) {
                if(qp && Math.Abs(v[i]-p[i])>0.001) qp=false;
                if(qm && Math.Abs(v[i]+p[i])>0.001) qm=false;
            }
            qr=qm;
            return qp||qm;
        }

        internal static double Dist2(double[] a,double[] b) {
            double r=0;
            for(int i=0;i<a.Length;i++) r+=(a[i]-b[i])*(a[i]-b[i]);
            return r;
        }

        internal static double Dist2Rev(double[] a,double[] b) {
            double r=0;
            for(int i=0;i<a.Length;i++) r+=(a[i]+b[i])*(a[i]+b[i]);
            return r;
        }

        internal static double[] ApplyMatrix(double[,] matr,double[] v) {
            int d=v.Length;
            double[] r=new double[d];
            for(int i=0;i<d;i++) {
                double h=0;
                for(int j=0;j<d;j++) h+=matr[j,i]*v[j];
                r[i]=h;
            }
            return r;
        }

        internal static double[] ApplyInvMatrix(double[,] matr,double[] v) {
            int d=v.Length;
            double[] r=new double[d];
            for(int i=0;i<d;i++) {
                double h=0;
                for(int j=0;j<d;j++) h+=matr[i,j]*v[j];
                r[i]=h;
            }
            return r;
        }

        internal static double[] ApplyMatrixToTwist(double[,] matr,double[] v) {
            int dim=matr.GetLength(0);
            int nseg=v.Length/dim;
            double[] r=new double[nseg*dim];
            for(int s=0;s<nseg;s++) {
                for(int i=0;i<dim;i++) {
                    double h=0;
                    for(int j=0;j<dim;j++) h+=matr[j,i]*v[s*dim+j];
                    r[s*dim+i]=h;
                }
            }
            return r;
        }

        internal static double[] ApplyMatrixToTwist(double[] matr, double[] v, int dim) {
            int nseg = v.Length / dim;
            double[] r = new double[nseg * dim];
            for(int s = 0; s < nseg; s++) {
                for(int i = 0; i < dim; i++) {
                    double h = 0;
                    for(int j = 0; j < dim; j++) h += v[s * dim + j] * matr[j * dim + i];
                    r[s * dim + i] = h;
                }
            }
            return r;
        }
        internal static double[,] GetMatrixForTwist(double[] rtw,double ang) {
            double c0=Math.Cos(ang),s0=Math.Sin(ang);
            int d=rtw.Length/2;
            double[,]m=new double[d,d];
            for(int i=0;i<d;i++) {
                for(int j=0;j<d;j++) {
                    m[i,j]=(i==j?1:0)+(rtw[i]*rtw[j]+rtw[i+d]*rtw[j+d])*(c0-1)+(rtw[i]*rtw[j+d]-rtw[i+d]*rtw[j])*s0;
                }
            }
            return m;
        }

        internal static double DotProd(double[] a,double[] b) {
            double r=0;
            for(int i=0;i<a.Length;i++) r+=a[i]*b[i];
            return r;
        }

        internal static void CloseMatrixSet(ArrayList S) {
            double[][,] mx=new double[8192][,];
            int p=0,q=0;
            foreach(double[,] a in S) mx[q++]=a;
            while(p<q) {
                double[,] m=mx[p++];
                for(int i=0;i<q;i++) {
                    double[,] m1=MatrixMul(m,mx[i]);
                    for(int j=0;j<q;j++) {
                        if(MatrixEqual(m1,mx[j])) goto _1;
                    }
                    if(q==mx.Length) throw new Exception("Too many matrices");
                    mx[q++]=m1; S.Add(m1);
_1: ;
                }
            }
        }

        internal static double[,] MatrixMul(double[,] m,double[,] p) {
            int d=m.GetLength(0);
            double[,] res=new double[d,d];
            for(int i=0;i<d;i++) {
                for(int j=0;j<d;j++) {
                    double s=0;
                    for(int k=0;k<d;k++) s+=m[i,k]*p[k,j];
                    res[i,j]=s;
                }
            }
            return res;
        }
        internal static double[,] MatrixMulInv(double[,] m,double[,] p) {  // m*p'
            int d=m.GetLength(0);
            double[,] res=new double[d,d];
            for(int i=0;i<d;i++) {
                for(int j=0;j<d;j++) {
                    double s=0;
                    for(int k=0;k<d;k++) s+=m[i,k]*p[j,k];
                    res[i,j]=s;
                }
            }
            return res;
        }
        internal static double[,] MatrixMulInv2(double[,] m,double[,] p) {  // m'*p
            int d=m.GetLength(0);
            double[,] res=new double[d,d];
            for(int i=0;i<d;i++) {
                for(int j=0;j<d;j++) {
                    double s=0;
                    for(int k=0;k<d;k++) s+=m[k,i]*p[k,j];
                    res[i,j]=s;
                }
            }
            return res;
        }

        internal static bool MatrixEqual(double[,] m,double[,] p) {
            int d=m.GetLength(0);
            for(int i=0;i<d;i++) {
                for(int j=0;j<d;j++) {
                    if(Math.Abs(m[i,j]-p[i,j])>1e-3) return false;
                }
            }
            return true;
        }

        // ─── Flat-matrix functions (for N-segment generator support) ───

        internal static double[] CreateMatrixIdent(int dim) {
            double[] m = new double[dim * dim];
            for(int i = 0; i < dim; i++) m[i * (dim + 1)] = 1.0;
            return m;
        }

        internal static double[] CreateMatrixFromTwist(double[] mov, int dim) {
            double[] m = CreateMatrixIdent(dim);
            return ApplyTwistN(mov, m, dim);
        }

        internal static double[] ApplyMatrix(double[] matr, double[] vec, int dim) {
            int nv = vec.Length;
            double[] r = new double[nv];
            for(int i = 0; i < nv; i += dim) {
                for(int j = 0; j < dim; j++) {
                    double h = 0;
                    for(int k = 0; k < dim; k++) h += vec[i + k] * matr[k * dim + j];
                    r[i + j] = h;
                }
            }
            return r;
        }

        internal static double[] ApplyInvMatrix(double[] matr, double[] vec, int dim) {
            int nv = vec.Length;
            double[] r = new double[nv];
            for(int i = 0; i < nv; i += dim) {
                for(int j = 0; j < dim; j++) {
                    double h = 0;
                    for(int k = 0; k < dim; k++) h += vec[i + k] * matr[j * dim + k];
                    r[i + j] = h;
                }
            }
            return r;
        }

        internal static bool MatrixEqual(double[] m1, double[] m2, out bool qr, int dim) {
            qr = false;
            bool qp = true, qm = true;
            for(int i = 0; i < dim; i++) {
                for(int j = 0; j < dim; j++) {
                    double d = m1[i * dim + j];
                    if(qp && Math.Abs(d - m2[i * dim + j]) > 0.001) qp = false;
                    if(qm && Math.Abs(d - m2[j * dim + i]) > 0.001) qm = false;
                    if(!qp && !qm) return false;
                }
            }
            qr = !qp;
            return true;
        }

        internal static void CloseMatrixSet(List<double[]> S, int dim) {
            int count = S.Count;
            for(int i = 0; i < S.Count; i++) {
                double[] a = S[i];
                for(int j = 0; j < count; j++) {
                    double[] p = ApplyMatrix(S[j], a, dim);
                    bool found = false;
                    foreach(double[] m in S) {
                        bool qr;
                        if(MatrixEqual(p, m, out qr, dim) && !qr) { found = true; break; }
                    }
                    if(!found) S.Add(p);
                }
                if(S.Count >= 100000) throw new Exception("Too many matrices");
            }
        }

        internal static double Angle(double[] tw, int idx, int dim) {
            double la = 0, lb = 0, pr = 0;
            for(int i = 0; i < dim; i++) {
                la += tw[idx + i] * tw[idx + i];
                lb += tw[idx + i + dim] * tw[idx + i + dim];
                pr += tw[idx + i] * tw[idx + i + dim];
            }
            return Math.Acos(Math.Min(1.0, pr / Math.Sqrt(la * lb)));
        }

        internal static double[] GetMatrixForTwist(double[] mtw, double cf, int dim) {
            double[] tmp = new double[dim];
            double[] m = CreateMatrixIdent(dim);
            int num = mtw.Length;
            int nseg = num / dim;
            for(int i = 0; i < num - dim; i += 2 * dim) {
                double ang = Angle(mtw, i, dim);
                if(nseg % 2 != 0 && Math.Abs(Math.Cos(ang)) < 0.01) {
                    ApplyMirror(mtw, i, cf, m, dim);
                    ApplyMirror(mtw, i + dim, cf, m, dim);
                } else {
                    double cfac = ang * cf;
                    double sn = Math.Sin(cfac) / Math.Sin(ang);
                    double cs = Math.Cos(cfac) - Math.Cos(ang) * sn;
                    for(int j = 0; j < dim; j++) tmp[j] = mtw[i + dim + j] * sn + mtw[i + j] * cs;
                    ApplyMirror(mtw, i, 1.0, m, dim);
                    ApplyMirror(tmp, 0, 1.0, m, dim);
                }
            }
            if(num % (2 * dim) != 0) ApplyMirror(mtw, num - dim, cf, m, dim);
            return m;
        }

        internal static void ApplyMirror(double[] rvec, int idx, double cf, double[] vecs, int dim) {
            double nn = 0;
            for(int i = 0; i < dim; i++) nn += rvec[idx + i] * rvec[idx + i];
            for(int j = 0; j < vecs.Length; j += dim) {
                double dot = 0;
                for(int k = 0; k < dim; k++) dot += rvec[idx + k] * vecs[j + k];
                double s = 2 * dot / nn * cf;
                for(int k = 0; k < dim; k++) vecs[j + k] -= s * rvec[idx + k];
            }
        }

        internal static double[] GetTwistPole(double[] Orig, double[] axis) {
            int num = Orig.Length;
            int d4 = num / 4;
            double[] p = new double[4];
            if(d4 % 2 != 0) {
                for(int i = 0; i < 4; i++) p[i] = Orig[num - 4 + i];
            } else {
                p[0] = det3(Orig, axis, 1, 2, 3);
                p[1] = det3(Orig, axis, 0, 3, 2);
                p[2] = det3(Orig, axis, 1, 3, 0);
                p[3] = det3(Orig, axis, 0, 2, 1);
            }
            return p;
        }

        private static double det3(double[] tw, double[] ax, int a, int b, int c) {
            return tw[a] * (tw[b + 4] * ax[c] - tw[c + 4] * ax[b])
                 + tw[b] * (tw[c + 4] * ax[a] - tw[a + 4] * ax[c])
                 + tw[c] * (tw[a + 4] * ax[b] - tw[b + 4] * ax[a]);
        }

        internal static double[] InvMatrix(double[] matr, int dim) {
            double[] r = new double[dim * dim];
            for(int i = 0; i < dim; i++)
                for(int j = 0; j < dim; j++)
                    r[i * dim + j] = matr[j * dim + i];
            return r;
        }
    }
}
