using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections;
using System.IO;

namespace _3dedit {
	class PuzzleStructure {
		const int MaxNVert=131072,MaxNAxes=4096;

		internal string Name;
		internal int Dim;
		internal PBaseAxis[] BaseAxes;
		internal PBaseFace[] BaseFaces;
		internal PAxis[] Axes;
		internal PFace[] Faces;
		internal int NStickers;
		internal bool QSimplified;
		internal string QGenerator; // "cube D N" for CubeGenerator puzzles, null for normal

		double[][] Group;

		internal PuzzleStructure(string name,string[] def) {
			Name=name;
			FillFromStrings(def);
			ExpandAxes();  // and set ID
			ExpandFaces();  // and set ID
			CutFaces();   // and set cutaxes for all faces
			EnumerateStickers();
			SortStickersForAxes();  // for base and all!
			CreateTwistMaps();
		}

		// Private empty constructor for factory methods (e.g. CreateCubeGenerated)
		private PuzzleStructure() { }

		/// <summary>Generate CubeBased N^D puzzle definition with standard cuts.</summary>
		private static string[] BuildCubeDef(int dim, int order) {
			var lines = new List<string>();
			lines.Add("Dim " + dim);
			lines.Add("NAxis 1");

			var sb = new StringBuilder();
			sb.Append("Faces 1");
			for (int i = 1; i < dim; i++) sb.Append(",0");
			lines.Add(sb.ToString());

			// Group: a = (1,0,...,0), b = (1,0,...,1_k,...,0) for k=2..dim
			sb.Length=0;
			sb.Append("Group");
			for (int k = 2; k <= dim; k++) {
				sb.Append(" 1");
				for (int i = 2; i <= dim; i++) sb.Append(",0");
				sb.Append("/1");
				for (int i = 2; i <= dim; i++) sb.Append(i == k ? ",1" : ",0");
			}
			lines.Add(sb.ToString());

			// Axis
			sb.Length=0;
			sb.Append("Axis 1");
			for (int i = 1; i < dim; i++) sb.Append(",0");
			lines.Add(sb.ToString());

			// Twists: use the EXACT definition from MPUlt_puzzles.txt
			// for D=4 (proven working with FindTwist/CheckTwist).
			// For D>4 we generate C(D-1,2) coordinate-plane twists.
			sb.Length=0;
			sb.Append("Twists");
			if (dim == 4) {
				// Exact copy of 2^4 / 3^4 twist set from puzzles.txt
				sb.Append(" 0,1,0,0/0,1,1,0 0,1,-1,0/0,0,0,1 0,2,-1,-1/0,1,1,-2");
			} else {
				for (int i = 2; i <= dim; i++) {
					for (int j = i + 1; j <= dim; j++) {
						sb.Append(" ");
						for (int p = 1; p <= dim; p++) {
							if (p > 1) sb.Append(",");
							sb.Append((p == i || p == j) ? "1" : "0");
						}
						sb.Append("/");
						for (int p = 1; p <= dim; p++) {
							if (p > 1) sb.Append(",");
							sb.Append(p == i ? "1" : "0");
						}
					}
				}
			}
			lines.Add(sb.ToString());

			// Cuts: equally spaced; order=1 → no cut values (single piece)
			sb.Length=0;
			if (order <= 1) {
				lines.Add("Cuts");
			} else {
				sb.Append("Cuts");
				for (int i = 0; i < order - 1; i++) {
					double cut = 1.0 - 2.0 * (i + 1) / order;
					sb.Append(" " + FormatNum(cut));
				}
				lines.Add(sb.ToString());
			}

			return lines.ToArray();
		}

		private void ExpandAxes() {
			int q=BaseAxes.Length;
			PAxis[] CAxes=new PAxis[MaxNAxes];
			for(int i=0;i<q;i++) {
				PBaseAxis ax=BaseAxes[i];
				ax.Id=i;
				ax.ExpandPrimaryTwists();
				CAxes[i]=new PAxis(ax); // matrix=id
			}
			for(int p=0;p<q;p++) {
				double[] R=CAxes[p].Dir;
				foreach(double []G in Group) {
					double[] v=PGeom.ApplyTwist(G,R);
					int j;
					bool qr;
					for(j=0;j<q;j++) if(PGeom.AxisEqual(v,CAxes[j].Dir,out qr)) break;
					if(j==q) {
						if(q==MaxNAxes) throw new Exception("Too many axes");
						CAxes[q]=new PAxis(CAxes[p],G);
						q++;
					}
				}
			}
			Axes=new PAxis[q];
			for(int i=0;i<q;i++) {
				Axes[i]=CAxes[i];
				Axes[i].Id=i;
			}
		}

		private void ExpandFaces() {
			PFace[] CFaces=new PFace[MaxNVert];
			int NRefl=Group.Length;
			int p,q=BaseFaces.Length;
			for(int i=0;i<q;i++) {
				BaseFaces[i].Id=i;
				CFaces[i]=new PFace(BaseFaces[i]);
			}
			for(p=0;p<q;p++) {
				foreach(double []G in Group) {
					double[] v=PGeom.ApplyTwist(G,CFaces[p].Pole);
					int j;
					for(j=0;j<q;j++) if(PGeom.VertEqual(v,CFaces[j].Pole)) {
						double[,] mf=PGeom.ApplyTwist(G,CFaces[p].Matrix);
						CFaces[p].Base.AddSMatrix(mf,CFaces[j].Matrix);
						break;
					}
					if(j==q) {
						if(q==MaxNVert) throw new Exception("Too many vertices");
						CFaces[q]=new PFace(CFaces[p],G);
						q++;
					}
				}
			}
			Faces=new PFace[q];
			for(int i=0;i<q;i++) {
				Faces[i]=CFaces[i];
				Faces[i].Id=i;
			}
			foreach(PBaseFace BF in BaseFaces) BF.CloseSMatrixSet();
		}

		private void CutFaces() {
			double RMax=0;
			foreach(PBaseFace F in BaseFaces) {
				RMax=Math.Max(RMax,PGeom.VLength(F.Pole));
			}
			int minrank=int.MaxValue;
			foreach(PBaseFace F in BaseFaces) {
				CutNode face=CutNode.GenCube(Dim,RMax*Dim);
				double[] hpln=CutNetwork.GetPlane(F.Pole,1);
				face.Split(1,hpln,false);
				face=face.ZeroNode;
				int opgen=1;
				foreach(PFace FF in Faces) if(F.Id!=FF.Id) {
					hpln=CutNetwork.GetPlane(FF.Pole,1);
					face.Split(++opgen,hpln,true);
					if(face.Status!=CutNode.STAT_PLUS) throw new Exception("Empty face: Id="+F.Id);
				}

				int nff=face.Children.Length;
				double[][] ffpol=new double[nff][];
				for(int i=0;i<nff;i++) ffpol[i]=face.Children[i].Pole;
				F.FPoles=ffpol;

				LMesh m=new LMesh(Dim,true);
				face.FillLMesh(++opgen,m);
				m.CloseCtr();
				F.FaceMesh=new PMesh(m);
				F.FaceMesh.FCtr=F.Pole;

				double[] verts=m.pts;
				int nverts=m.npts;
				CutNetwork CN=new CutNetwork(face,Dim,opgen);
				double[][] fctrs=null;
				if(QSimplified) fctrs=CN.GetCtrs();
				F.AxisLayers=new int[Axes.Length];
				for(int u=0;u<Axes.Length;u++){
					PAxis Ax=Axes[u];
					double[] D=Ax.Dir;
					double lD=PGeom.Dist2(D,new double[Dim]);
					double smin=double.MaxValue,smax=double.MinValue;
					for(int i=0;i<nverts;i++) {
						double v=0;
						for(int j=0;j<Dim;j++) v+=D[j]*verts[i*Dim+j];
						if(v<smin) smin=v;
						if(v>smax) smax=v;
					}
					smin/=lD; smax/=lD;
					bool cs=false,cc=false;
					int k=Ax.Base.NLayers-1;
					for(int i=0;i<Ax.Base.Cut.Length;i++) {
						double p=Ax.Base.Cut[i];
						if(p>=smax-0.0001) continue;
						if(!cs) { k=i; cs=true; }
						if(p<=smin+0.0001) break;
						hpln=CutNetwork.GetPlane(Ax.Dir,p);
						CN.Split(hpln,false);
						cc=true;
					}
					if(cc) { k=-1; }
					F.AxisLayers[u]=k;
				}
				F.SetStickers(m,CN,Axes,fctrs);
				minrank=Math.Min(minrank,F.MinRank());
			}
			foreach(PBaseFace F in BaseFaces) F.SubRank(minrank);
		}

		private void EnumerateStickers() {
			int s0=0;
			foreach(PFace F in Faces) {
				int ns=F.Base.NStickers;
				F.FirstSticker=s0;
				F.RefAxis=FindNearestAxis(F.Pole,true);
				int nca=F.Base.CutAxes.Length;
				F.CutAxes=new int[nca];
				for(int i=0;i<nca;i++) {
					F.CutAxes[i]=FindAxis(Axes[F.Base.CutAxes[i]].Dir,F.Matrix);
				}
				s0+=ns;
			}
			NStickers=s0;
		}

		private unsafe void SortStickersForAxes() {
			int[]S=new int[1000];
			foreach(PBaseAxis Ax in BaseAxes) {
				int nl=Ax.NLayers;
				int mask=Ax.FixedMask;
				Ax.Layers=new int[nl][];
				for(int i=0;i<nl;i++) {
					if(((mask>>i)&1)==0) {
						int ps=0;
						foreach(PFace F in Faces) {
							int ax=FindAxisInv(Ax.Dir,F.Matrix);
							bool qr=ax<0;
							ax=Math.Abs(ax)-1;
							PBaseFace BF=F.Base;
							int i1=qr ? nl-1-i : i;
							int h=BF.AxisLayers[ax];
							int nstk=BF.NStickers;
							int fstk=F.FirstSticker;
							if(h>=0) {
								if(i1!=h) continue;
								for(int j=0;j<nstk;j++) S=SetInt(S,ps++,fstk+j);
							}else{
								int r=Array.IndexOf<int>(BF.CutAxes,ax);
								if(r<0) throw new Exception("Can't find CutAxis");
								byte[,] aa=BF.StickerMask;
								for(int j=0;j<nstk;j++) if(aa[j,r]==i1) S=SetInt(S,ps++,fstk+j);
							}
						}
						int[] ar=new int[ps];
						Buffer.BlockCopy(S,0,ar,0,sizeof(int)*ps);
						Ax.Layers[i]=ar;
					}
				}
			}
			PermByMatr perm=new PermByMatr(Axes.Length,Faces.Length);
			foreach(PAxis Ax in Axes) {
				Ax.Layers=ConvertStickersFromLayers(Ax.Base,Ax.Matrix,perm);
			}
		}

		static int[] SetInt(int[] arr,int ind,int val) {
			if(arr.Length<=ind) {
				int[] arr1=new int[2*ind+2];
				Buffer.BlockCopy(arr,0,arr1,0,arr.Length*sizeof(int));
				arr=arr1;
			}
			arr[ind]=val;
			return arr;
		}

		private int[][] ConvertStickersFromLayers(PBaseAxis Ax,double[,] matr,PermByMatr P) {
			for(int i=0;i<Axes.Length;i++) {
				P.CvAxes[i]=FindAxis(Axes[i].Dir,matr);
			}
			for(int i=0;i<Faces.Length;i++) P.CvFaces[i]=FindFace(Faces[i].Pole,matr);

			int[][] res=new int[Ax.NLayers][];
			for(int i=0;i<Ax.NLayers;i++) {
				if(Ax.Layers[i]!=null) {
					int[] rres=(int[])Ax.Layers[i].Clone();

					int f0=-1,f1=-1;
					int nca=0;
					for(int j=0;j<rres.Length;j++) {
						int h=rres[j];
						bool cf=false;
						while(f0<Faces.Length-1 && h>=Faces[f0+1].FirstSticker) { f0++; cf=true; }
						if(cf) {
							f1=P.CvFaces[f0];
							nca=Faces[f0].Base.NCutAxes;
							P.ReallocPerm(nca);
							for(int k=0;k<nca;k++) {
								int rax=Faces[f0].CutAxes[k];
								int pax=Math.Abs(rax)-1;
								int s=rax<0 ? -P.CvAxes[pax] : P.CvAxes[pax];
								for(int l=0;l<nca;l++) {
									int s1=Faces[f1].CutAxes[l];
									if(s1==s || s1==-s) {
										P.PermAxes[k]=l;
										P.InvAxes[k]=(s1!=s) ? Axes[pax].Base.NLayers-1 : 0;
										s=0;
										break;
									}
								}
								if(s!=0) throw new Exception("Can't find converted cutting axis");
							}
						}
						int ns=h-Faces[f0].FirstSticker;
						for(int k=0;k<nca;k++) {
							int m=Faces[f0].Base.StickerMask[ns,k];
							if(P.InvAxes[k]!=0) m=P.InvAxes[k]-m;
							P.StkMask[P.PermAxes[k]]=m;
						}
						rres[j]=Faces[f1].FirstSticker+Faces[f0].Base.FindByMask(P.StkMask);
					}
					res[i]=rres;
				}
			}
			return res;
		}

		private void CreateTwistMaps() {
			PermByMatr perm=new PermByMatr(Axes.Length,Faces.Length);
			foreach(PBaseAxis Ax in BaseAxes) {
				foreach(PBaseTwist tw in Ax.Twists) {
					double[,] matr=PGeom.ApplyTwist(tw.Dir,PGeom.MatrixIdentity(Dim));
					int[][] R=ConvertStickersFromLayers(Ax,matr,perm);
					int[][] R1=new int[Ax.NLayers][];
					for(int i=0;i<Ax.NLayers;i++) {
						if(R[i]!=null) {
							R1[i]=new int[R[i].Length];
							for(int j=0;j<R[i].Length;j++) {
								int h=Array.BinarySearch<int>(Ax.Layers[i],R[i][j]);
								if(h<0) throw new Exception("Can't find sticker image in twist");
								R[i][j]=h; R1[i][h]=j;
							}
						}
					}
					tw.Map=R; tw.InvMap=R1;
				}
			}
		}

		private void FillFromStrings(string[] descr) {
			int naxis=0;
			int caxis=0;
			PBaseAxis ax=null;
			int state=0;
			int nv;
			int fline=0;
			QSimplified=false;
			try {
				while(state>=0) {
					if(fline==descr.Length) {
						if(state!=7) throw new Exception("Unexpected end");
						break;
					}
					string line=descr[fline++];
					if(line=="" || line==null || line[0]=='#') continue;
					string[] str=line.Split(' ','\t');
					if(str.Length==0 || str[0]=="" || str[0]==null) continue;
					string cmd=str[0].ToLowerInvariant();
					switch(state) {
						case 0:
							if(cmd!="dim") throw new Exception("'Dim' required");
							Dim=int.Parse(str[1]);
							break;
						case 1:
							if(cmd!="naxis") throw new Exception("'NAxis' required");
							naxis=int.Parse(str[1]);
							BaseAxes=new PBaseAxis[naxis];
							break;
						case 2:
							if(cmd!="faces") throw new Exception("'Faces' required");
							nv=str.Length-1;
							BaseFaces=new PBaseFace[nv];
							for(int i=0;i<nv;i++) {
								BaseFaces[i]=new PBaseFace(GetVector(str[i+1],Dim));
							}
							break;
						case 3:
							if(cmd=="simplified") { QSimplified=true; state--; break; }
							if(cmd!="group") throw new Exception("'Group' required");
							nv=str.Length-1;
							Group=new double[nv][];
							for(int i=0;i<nv;i++) {
								Group[i]=GetVector(str[i+1],2*Dim);
								PGeom.GetOrder(Group[i]);
							}
							if(naxis==0) state=-2;
							break;
						case 4:
							if(cmd!="axis") throw new Exception("'Axis' required");
							ax=new PBaseAxis(GetVector(str[1],Dim));
							break;
						case 5:
							if(cmd!="twists") throw new Exception("'Twists' required");
							nv=str.Length-1;
							ax.Twists=new PBaseTwist[nv];
							for(int i=0;i<nv;i++) {
								ax.Twists[i]=new PBaseTwist(GetVector(str[i+1],2*Dim));
							}
							break;
						case 6:
							if(cmd!="cuts") throw new Exception("'Cuts' required");
							nv=str.Length-1;
							ax.Cut=new double[nv];
							for(int i=0;i<nv;i++) ax.Cut[i]=double.Parse(str[i+1],CultureInfo.InvariantCulture);
							ax.AdjustCuts();
							BaseAxes[caxis++]=ax;
							ax=null;
							break;
						case 7: {
							if(cmd=="fixedmask") {
								BaseAxes[caxis-1].FixedMask=int.Parse(str[1]);
								if(caxis==naxis) state=-2;
								else state=3;
								break;
							}
							if(caxis!=naxis) { state=4; goto case 4; }
							state=-2;
							break;
						}
					}
					state++;
				}
			} catch(Exception e) {
				throw new Exception("Error: "+e.Message+" in line "+fline+": "+(descr[fline] ?? "{null}"));
			}
		}

		static double[] GetVector(string s,int dim) {
			string[] str=s.Split(',','/','>');
			double[] res=new double[dim];
			for(int i=0;i<dim;i++) res[i]=double.Parse(str[i],CultureInfo.InvariantCulture);
			return res;
		}

		internal void SetColors(short[] Field) {
			int c=0;
			foreach(PFace F in Faces) {
				int s0=F.FirstSticker;
				int n=F.Base.NStickers;
				for(int i=0;i<n;i++) Field[s0+i]=(short)c;
				c++;
			}
		}

		internal bool Check(short[] Field,short mask) {
			foreach(PFace F in Faces) {
				int s0=F.FirstSticker;
				int n=F.Base.NStickers;
				short c0=Field[s0];
				for(int i=1;i<n;i++) if(((Field[s0+i]^c0)&mask)!=0) return false;
			}
			return true;
		}

		internal static string[] ExampleDescr=new string[]{
			"Dim 4",
			"NAxis 1",
			"Faces 1,0,0,0",
			"Group 1,0,0,0/1,1,0,0 1,0,0,0/1,0,1,0 1,0,0,0/1,0,0,1",
			"Axis 1,0,0,0",
			"Twists 0,1,0,0/0,1,1,0 0,1,-1,0/0,0,0,1 0,2,-1,-1/0,1,1,-2",
			"Cuts 0.33 -0.33"};

		internal static PuzzleStructure Example {
			get {
				return new PuzzleStructure("Cube4D_3_FT",ExampleDescr);
			}
		}

		internal string[] GetDescription() {
			ArrayList arr=new ArrayList();
			if(QGenerator!=null) arr.Add("#Generator: "+QGenerator);
			arr.Add("Dim "+Dim);
			arr.Add("NAxis "+BaseAxes.Length);
			string sf;
			sf="Faces";
			foreach(PBaseFace F in BaseFaces) sf+=" "+Vector2Text(F.Pole);
			arr.Add(sf);
			if(QSimplified) arr.Add("Simplified");
			sf="Group";
			foreach(double[] G in Group) sf+=" "+Twist2Text(G);
			arr.Add(sf);
			foreach(PBaseAxis Ax in BaseAxes) {
				arr.Add("Axis "+Vector2Text(Ax.Dir));
				sf="Twists";
				for(int i=0;i<Ax.NPrimaryTwists;i++) sf+=" "+Twist2Text(Ax.Twists[i].Dir);
				arr.Add(sf);
				sf="Cuts";
				foreach(double m in Ax.Cut) sf+=" "+FormatNum(m);
				arr.Add(sf);
				if(Ax.FixedMask!=0) arr.Add("FixedMask "+Ax.FixedMask);
			}
			return (string[])arr.ToArray(typeof(string));
		}

		private string Twist2Text(double[] G) {
			string h="";
			int d=G.Length/2;
			for(int i=0;i<2*d;i++) {
				if(i==d) h+="/";
				else if(i!=0) h+=",";
				h+=FormatNum(G[i]);
			}
			return h;
		}

		private string Vector2Text(double[] p) {
			string h="";
			int d=p.Length;
			for(int i=0;i<d;i++) {
				if(i!=0) h+=",";
				h+=FormatNum(p[i]);
			}
			return h;
		}

		// Format a double value: integers as "N", floats as "F12".
		static string FormatNum(double v) {
			double r=Math.Round(v);
			if(Math.Abs(v-r)<1e-10) return r.ToString(CultureInfo.InvariantCulture);
			return v.ToString("F12",CultureInfo.InvariantCulture);
		}

		// Lightweight validation: check field keyword order matches FillFromStrings'
		// state machine without parsing values. First reads NAxis value to know
		// how many (Axis→Twists→Cuts→[FixedMask]) cycles to expect.
		internal static bool ValidateDefinitionFormat(string[] descr) {
			if(descr==null || descr.Length==0) return false;
			// Read NAxis value to determine cycle count.
			int naxis=1;
			for(int i=0;i<descr.Length;i++) {
				string t=descr[i].Trim();
				if(t.Length==0 || t[0]=='#') continue;
				string[] sp=t.Split(' ','\t');
				if(sp[0].ToLowerInvariant()=="naxis" && sp.Length>=2) {
					if(!int.TryParse(sp[1],out naxis) || naxis<1)
						return false;
					break;
				}
			}
			// Validate keyword order.
			// 0=Dim,1=NAxis,2=Faces,3=Group,4=Axis,5=Twists,6=Cuts,7=afterCuts,8=done
			int state=0,axisCnt=0;
			bool afterCuts=false;
			for(int i=0;i<descr.Length;i++) {
				string t=descr[i].Trim();
				if(t.Length==0 || t[0]=='#') continue;
				string cmd=t.Split(' ','\t')[0].ToLowerInvariant();
				if(state==0) {
					if(cmd!="dim") return false;
					state=1;
				} else if(state==1) {
					if(cmd!="naxis") return false;
					state=2;
				} else if(state==2) {
					if(cmd!="faces") return false;
					state=3;
				} else if(state==3) {
					if(cmd=="simplified") continue;
					if(cmd!="group") return false;
					state=4;
				} else if(state==4) {
					if(cmd!="axis") return false;
					axisCnt++; state=5;
				} else if(state==5) {
					if(cmd!="twists") return false;
					state=6;
				} else if(state==6) {
					if(cmd!="cuts") return false;
					afterCuts=true;
					state=axisCnt<naxis ? 7 : 8;
				} else if(state==7) {
					// Between axes: FixedMask → stay, Axis → next cycle
					if(cmd=="fixedmask") continue;
					if(cmd=="axis") { axisCnt++; state=5; continue; }
					return false;
				} else if(state==8) {
					// After all axes: only optional FixedMask
					if(cmd=="fixedmask") continue;
					return false;
				}
			}
			return afterCuts && axisCnt==naxis;
		}

		internal static PuzzleStructure ReadCompiled(string p) {
			return null;
			//throw new NotImplementedException();
		}

		internal static PuzzleStructure Create(string name,string[] descr) {
			// Check for generator comment — allows loading generated .log files
			// without going through GenCube (which OOMs at high dimensions).
			// Format: #Generator: <cube|cross> <dim> <order>
			foreach(string ln in descr) {
				if(ln.StartsWith("#Generator: ")) {
					string[] parts=ln.Split(' ');
					if(parts.Length<4) continue;
					string gen=parts[1];
					int dim=int.Parse(parts[2]);
					int order=int.Parse(parts[3]);
					PuzzleStructure ps=null;
					if(gen=="cube") ps=CreateCubeGenerated(dim,order);
					else if(gen=="cross") ps=CreateCrossGenerated(dim,order);
					else if(gen=="simplex") ps=CreateSimplexGenerated(dim,order);
					else continue;
					ps.Name=name;
					return ps;
				}
			}
			return new PuzzleStructure(name,descr);
		}

		internal void SaveCompiled(string p) {
			//throw new NotImplementedException();
		}

		internal int FindAxis(double[] dir,double[,] matr) {
			dir=PGeom.ApplyMatrix(matr,dir);
			return FindAxis(dir);
		}
		private int FindAxisInv(double[] dir,double[,] matr) {
			dir=PGeom.ApplyInvMatrix(matr,dir);
			return FindAxis(dir);
		}

		private int FindAxis(double[] dir) {
			int r0=0;
			double d0=double.MaxValue;
			foreach(PAxis Ax in Axes) {
				double v=PGeom.Dist2(dir,Ax.Dir);
				if(v<d0) { d0=v; r0=Ax.Id+1; }
				v=PGeom.Dist2Rev(dir,Ax.Dir);
				if(v<d0) { d0=v; r0=-Ax.Id-1; }
			}
			return r0;
		}

		private int FindFace(double[] p,double[,] matr) {
			p=PGeom.ApplyMatrix(matr,p);
			int r0=0;
			double d0=double.MaxValue;
			foreach(PFace F in Faces) {
				double v=PGeom.Dist2(p,F.Pole);
				if(v<d0) { d0=v; r0=F.Id; }
			}
			return r0;
		}

		internal bool FindTwist(int nf,double[] pt,out int ax,out int tw) { // 4D only
			ax=Faces[nf].RefAxis;
			tw=0;
			if(ax==0) return false;
			int iax=Math.Abs(ax)-1;
			double[] actr=Axes[iax].Dir;
			double[] rtw=new double[Dim];
			double l1=PGeom.DotProd(pt,actr)/PGeom.DotProd(actr,actr);
			for(int i=0;i<Dim;i++) rtw[i]=pt[i]-l1*actr[i];
			double tbest=-1;
			double[] p=new double[4];
			for(int i=0;i<Axes[iax].Twists.Length;i++) {
				double[] u=Axes[iax].Twists[i];
				p[0]=actr[1]*(u[2]*u[7]-u[3]*u[6])+actr[2]*(u[3]*u[5]-u[1]*u[7])+actr[3]*(u[1]*u[6]-u[2]*u[5]);
				p[1]=actr[0]*(u[3]*u[6]-u[2]*u[7])+actr[2]*(u[0]*u[7]-u[3]*u[4])+actr[3]*(u[2]*u[4]-u[0]*u[6]);
				p[2]=actr[0]*(u[1]*u[7]-u[3]*u[5])+actr[1]*(u[3]*u[4]-u[0]*u[7])+actr[3]*(u[0]*u[5]-u[1]*u[4]);
				p[3]=actr[0]*(u[2]*u[5]-u[1]*u[6])+actr[1]*(u[0]*u[6]-u[2]*u[4])+actr[2]*(u[1]*u[4]-u[0]*u[5]);
				l1=-PGeom.DotProd(p,rtw)/Math.Sqrt(PGeom.DotProd(p,p));
				if(l1<-tbest) {
					tbest=-l1; tw=-i-1;
				}
				if(l1>tbest) {
					tbest=l1; tw=i+1;
				}
			}
			return true;
		}


		internal void DebugPrint(TextWriter tw) {
			tw.WriteLine("Name={0}",Name);
			tw.WriteLine("Dim={0}",Dim);
			tw.WriteLine("NStickers={0}",NStickers);

			tw.Write("Group:");
			foreach(double[] G in Group) PrintVec(G,tw,true);
			tw.WriteLine();
			tw.WriteLine("Base Axes: {0}",BaseAxes.Length);
			foreach(PBaseAxis p in BaseAxes) p.DebugPrint(tw);
			tw.WriteLine("Base Faces: {0}",BaseFaces.Length);
			foreach(PBaseFace p in BaseFaces) p.DebugPrint(tw);
			tw.WriteLine("Axes: {0}",BaseAxes.Length);
			foreach(PAxis p in Axes) p.DebugPrint(tw);
			tw.WriteLine("Faces: {0}",BaseFaces.Length);
			foreach(PFace p in Faces) p.DebugPrint(tw);
		}
		internal static void PrintVec(double[] v,TextWriter tw,bool qh) {
			int d=v.Length;
			int d1=qh ? d/2 : d;
			for(int i=0;i<d;i++) {
				tw.Write("{0}{1:F4}",i==0 ? ' ' : i==d1 ? '/' : ',',v[i]);
			}
		}


		internal static void PrintIArr(int[] p,TextWriter tw) {
			foreach(int a in p) tw.Write(" {0}",a);
		}

		internal int FindNearestAxis(double[] pt,bool qexact) {
			double tbest=qexact ? 0.999999 : -1;
			int res=0;
			double ld=PGeom.DotProd(pt,pt);
			for(int i=0;i<Axes.Length;i++) {
				double[] v=Axes[i].Dir;
				double x=PGeom.DotProd(pt,v)/Math.Sqrt(ld*PGeom.DotProd(v,v));
				if(x>tbest) {
					res=i+1; tbest=x;
				}
				if(x<-tbest) {
					res=-i-1; tbest=-x;
				}
			}
			return res;
		}

		internal int FindFaceBySticker(int st) {
			int r=0;
			while(r<Faces.Length && Faces[r].FirstSticker<=st) r++;
			return r-1;
		}
		internal int BaseSticker(int st,int ax) {
			int rax=Math.Abs(ax)-1;
			PAxis p=Axes[rax];
			int lv=ax>0 ? 0 : p.Layers.Length-1;
			int[] L=p.Layers[lv];
			for(int i=0;i<L.Length;i++) if(L[i]==st) return i;
			throw new Exception("Can't find sticker in Layer");
		}
		internal int CheckTwist(int ax,int[] stkseq,int lstkseq,short[] fld,out int tw) {
			for(int i=0;i<NStickers;i++) fld[i]&=0x7fff;
			int rax=Math.Abs(ax)-1;
			tw=0;

			PAxis p=Axes[rax];
			int lv=ax>0 ? 0 : p.Base.NLayers-1;
			int[]L=p.Layers[lv],LB=p.Base.Layers[lv];
			int ntw=p.Twists.Length;
			int[] tws=new int[ntw*2];
			int ntws=0;
			for(int i=0;i<ntw;i++) {
				int[] LT=p.Base.Twists[i].Map[lv];
				bool q=true,q1=true;
				for(int a=0;a<lstkseq-1;a++) {
					int h=stkseq[a],h1=stkseq[a+1];
					if(h<0 || h1<0) continue;
					if(LT[h]!=h1) q=false;
					if(LT[h1]!=h) q1=false;
					if(!q && !q1) break;
				}
				if(q) tws[ntws++]=i+1;
				if(q1) tws[ntws++]=-i-1;
			}
			if(ntws==0) throw new Exception("No twists for stkseq");
			if(ntws==1) {
				tw=ax<0 ? -tws[0] : tws[0]; return 2; // finita
			}
			if(lstkseq!=0) {
				int a=stkseq[lstkseq-1];
				if(a>=0) {
					int nnext=0;
					int _b=0;
					for(int b=0;b<ntws;b++) {
						int t=tws[b];
						int b1=t>0 ? p.Base.Twists[t-1].Map[lv][a] : p.Base.Twists[-t-1].InvMap[lv][a];
						b1=L[b1];
						if((fld[b1]&0x8000)==0) {
							nnext++; fld[b1]|=unchecked((short)0x8000);
							_b=b1;
						}
					}
					if(nnext>1) return 0; // continue;
					fld[_b]&=0x7fff;
				}
			}
			int nq=0;
			for(int a=0;a<LB.Length;a++){
				int _b=-1;
				bool qc=false;
				for(int b=0;b<ntws;b++) {
					int t=tws[b];
					int b1=t>0 ? p.Base.Twists[t-1].Map[lv][a] : p.Base.Twists[-t-1].InvMap[lv][a];
					if(_b<0) _b=b1;
					else if(_b!=b1) { qc=true; break; }
				}
				if(qc) {
					fld[L[a]]|=unchecked((short)0x8000);
					nq++;
				}
			}
			if(nq!=0) return 1;
			tw=ax<0 ? -tws[0] : tws[0]; return 2;
		}

		internal void HighlightPiece(short[] fld,int stk) {
			if(stk<0) {
				for(int i=0;i<NStickers;i++) fld[i]&=0x7fff;
				return;
			}

			short[] mm=new short[NStickers];
			int cnt=0;
			foreach(PAxis ax in Axes) {
				foreach(int[] ll in ax.Layers) {
					if(ll!=null) {
						foreach(int k in ll) {
							if(k==stk) {
								foreach(int k1 in ll) mm[k1]++;
								cnt++;
								goto _1;
							}
						}
					}
				}
				foreach(int[] ll in ax.Layers) {
					if(ll!=null) {
						foreach(int k in ll) mm[k]--;
					}
				}
_1: ;
			}
			for(int i=0;i<NStickers;i++) {
				if(mm[i]==cnt) fld[i]|=(short)-0x8000;
				else fld[i]&=0x7fff;
			}
		}

		internal double GetRad() {
			double R=0;
			foreach(PBaseFace F in BaseFaces) {
				foreach(PMesh M in F.StickerMesh) {
					if(M.MinBDim==0) {
						double r=PGeom.VLength(M.Ctr);
						// Also check vertex extents — single-piece stickers have
						// center at face centroid but vertices extend further.
						for(int i=0;i<M.NV*M.PDim;i+=M.PDim) {
							double vr=0;
							for(int j=0;j<M.PDim;j++) vr+=M.Coords[i+j]*M.Coords[i+j];
							vr=Math.Sqrt(vr);
							if(vr>r) r=vr;
						}
						if(r>R) R=r;
					}
				}
			}
			return R;
		}

		internal double[,] GetBestMatrix(int f0,double[] p0,int f1,double[] p1) {
			// move from (f0,p0) to (f1,p1); point p is on the cell surface.
			PFace F0=Faces[f0],F1=Faces[f1];
			if(F0.Base.Id!=F1.Base.Id) return null;
			double[] q0=PGeom.ApplyInvMatrix(F0.Matrix,p0);
			double[] q1=PGeom.ApplyInvMatrix(F1.Matrix,p1);
			double dbest=double.MaxValue;
			double[,] mr=null;
			foreach(double[,] m in F0.Base.SMatrices) {
				double[] qq=PGeom.ApplyMatrix(m,q0);
				double v=PGeom.Dist2(qq,q1);
				if(v<dbest) { dbest=v; mr=m; }
			}
			return PGeom.MatrixMulInv2(F0.Matrix,PGeom.MatrixMul(mr,F1.Matrix));
		}

// ═══════════════════════════════════════════════════════════════
//  CubeGenerator — factory method + direct mesh generation
// ═══════════════════════════════════════════════════════════════

		/// <summary>
		/// Create a CubeBased N^D puzzle, using direct mesh generation for
		/// high dimensions (avoids GenCube 3^D OOM).
		/// </summary>
		public static PuzzleStructure CreateCubeGenerated(int dim,int order) {
			string name=order+"^"+dim;
			string[] def=BuildCubeDef(dim,order);

			// Always use direct generation — CubeBased coordinates are
			// known analytically, no GenCube needed at any dimension.
			var ps=new PuzzleStructure();
			ps.QGenerator="cube "+dim+" "+order;
			ps.Name=name;
			ps.Dim=dim;
			ps.FillFromStrings(def);
			ps.ExpandAxes();
			ps.ExpandFaces();
			ps.GenCubeFaces();
			ps.EnumerateStickers();
			ps.SortStickersForAxes();
			ps.CreateTwistMaps();
			return ps;
		}

		/// <summary>Check whether all base faces are axis-aligned (CubeBased).</summary>
		private bool IsCubeBased() {
			foreach(PBaseFace F in BaseFaces) {
				int nz=0;
				for(int j=0;j<Dim;j++) if(Math.Abs(F.Pole[j])>0.001) nz++;
				if(nz!=1) return false;
			}
			return true;
		}

		/// <summary>
		/// True iff all cut values are at or outside ±|Pole| for CubeBased faces.
		/// In this case each face produces exactly one sticker and we can use the
		/// direct mesh path, avoiding GenCube completely.
		/// </summary>
		private bool IsSinglePieceFace(PBaseFace F) {
			if(!IsCubeBased()) return false;
			double R=PGeom.VLength(F.Pole);
			foreach(PAxis Ax in Axes) {
				foreach(double cut in Ax.Base.Cut) {
					if(cut>-R+0.0001 && cut<R-0.0001) return false;
				}
			}
			return true;
		}

		/// <summary>
		/// CutFaces replacement for the CubeGenerator high-dim path.
		/// Generates face geometry directly — no GenCube.
		/// </summary>
		private void GenCubeFaces() {
			double RMax=0;
			foreach(PBaseFace F in BaseFaces) {
				RMax=Math.Max(RMax,PGeom.VLength(F.Pole));
			}
			int minrank=int.MaxValue;
			foreach(PBaseFace F in BaseFaces) {
				if(IsSinglePieceFace(F)) {
					GenerateMinimalFace(F,RMax);
					minrank=Math.Min(minrank,F.MinRank());
					continue;
				}
				// Multi-cut case at high dim — will be rare; run GenCube for now.
				CutNode face=CutNode.GenCube(Dim,RMax*Dim);
				double[] hpln=CutNetwork.GetPlane(F.Pole,1);
				face.Split(1,hpln,false);
				face=face.ZeroNode;
				int opgen=1;
				foreach(PFace FF in Faces) if(F.Id!=FF.Id) {
					hpln=CutNetwork.GetPlane(FF.Pole,1);
					face.Split(++opgen,hpln,true);
					if(face.Status!=CutNode.STAT_PLUS) throw new Exception("Empty face: Id="+F.Id);
				}

				int nff=face.Children.Length;
				double[][] ffpol=new double[nff][];
				for(int i=0;i<nff;i++) ffpol[i]=face.Children[i].Pole;
				F.FPoles=ffpol;

				LMesh m=new LMesh(Dim,true);
				face.FillLMesh(++opgen,m);
				m.CloseCtr();
				F.FaceMesh=new PMesh(m);
				F.FaceMesh.FCtr=F.Pole;

				double[] verts=m.pts;
				int nverts=m.npts;
				CutNetwork CN=new CutNetwork(face,Dim,opgen);
				double[][] fctrs=null;
				if(QSimplified) fctrs=CN.GetCtrs();
				F.AxisLayers=new int[Axes.Length];
				for(int u=0;u<Axes.Length;u++){
					PAxis Ax=Axes[u];
					double[] D=Ax.Dir;
					double lD=PGeom.Dist2(D,new double[Dim]);
					double smin=double.MaxValue,smax=double.MinValue;
					for(int i=0;i<nverts;i++) {
						double v=0;
						for(int j=0;j<Dim;j++) v+=D[j]*verts[i*Dim+j];
						if(v<smin) smin=v;
						if(v>smax) smax=v;
					}
					smin/=lD; smax/=lD;
					bool cs=false,cc=false;
					int k=Ax.Base.NLayers-1;
					for(int i=0;i<Ax.Base.Cut.Length;i++) {
						double p=Ax.Base.Cut[i];
						if(p>=smax-0.0001) continue;
						if(!cs) { k=i; cs=true; }
						if(p<=smin+0.0001) break;
						hpln=CutNetwork.GetPlane(Ax.Dir,p);
						CN.Split(hpln,false);
						cc=true;
					}
					if(cc) { k=-1; }
					F.AxisLayers[u]=k;
				}
				F.SetStickers(m,CN,Axes,fctrs);
				minrank=Math.Min(minrank,F.MinRank());
			}
			foreach(PBaseFace F in BaseFaces) F.SubRank(minrank);
		}

		/// <summary>
		/// Generate a minimal (D-1)-cube face mesh directly (no GenCube).
		/// Used for single-piece CubeBased faces at high dimension.
		/// </summary>
		private void GenerateMinimalFace(PBaseFace F,double R) {
			int dim=Dim-1;

			int faceDir=0;
			for(int j=0;j<Dim;j++) if(Math.Abs(F.Pole[j])>0.001) { faceDir=j; break; }
			double fsign=F.Pole[faceDir]>0 ? 1 : -1;

			int[] freeDims=new int[dim];
			int fi=0;
			for(int j=0;j<Dim;j++) if(j!=faceDir) freeDims[fi++]=j;

			// vertices
			int nverts=1<<dim;
			int[] vIdx=new int[nverts];
			LMesh m=new LMesh(Dim,true);
			for(int v=0;v<nverts;v++) {
				double[] pt=new double[Dim];
				pt[faceDir]=fsign*R;
				for(int i=0;i<dim;i++) {
					pt[freeDims[i]]=((v>>i)&1)!=0 ? R : -R;
				}
				vIdx[v]=m.AddPoint(pt,0);
			}

			// edges
			for(int v0=0;v0<nverts;v0++) {
				for(int i=0;i<dim;i++) {
					int v1=v0^(1<<i);
					if(v1>v0) m.AddSeg(vIdx[v0],vIdx[v1]);
				}
			}

			// triangles: each 2D face of the (D-1)-cube → 2 triangles
			for(int i=0;i<dim;i++) {
				for(int j=i+1;j<dim;j++) {
					for(int v=0;v<nverts;v++) {
						if(((v>>i)&1)==0 && ((v>>j)&1)==0) {
							int v00=vIdx[v];
							int v01=vIdx[v|(1<<i)];
							int v10=vIdx[v|(1<<j)];
							int v11=vIdx[v|(1<<i)|(1<<j)];
							m.AddTrg(v00,v01,v11);
							m.AddTrg(v00,v11,v10);
						}
					}
				}
			}
			m.CloseCtr();

			F.FaceMesh=new PMesh(m);
			F.FaceMesh.FCtr=F.Pole;

			// FPoles
			F.FPoles=new double[2*dim][];
			for(int i=0;i<dim;i++) {
				double[] p1=new double[Dim];
				double[] p2=new double[Dim];
				p1[freeDims[i]]=1;
				p2[freeDims[i]]=-1;
				F.FPoles[2*i]=p1;
				F.FPoles[2*i+1]=p2;
			}

			// AxisLayers: compute k matching original cut-loop logic
			double[] vertCoords=m.pts;
			int nVertCoords=m.npts;
			F.AxisLayers=new int[Axes.Length];
			int rnk=0;
			for(int u=0;u<Axes.Length;u++) {
				double[] D=Axes[u].Dir;
				double lD=0;
				for(int j=0;j<Dim;j++) lD+=D[j]*D[j];
				double smax=double.MinValue,smin=double.MaxValue;
				for(int i=0;i<nVertCoords;i++) {
					double v=0;
					for(int j=0;j<Dim;j++) v+=D[j]*vertCoords[i*Dim+j];
					if(v<smin) smin=v;
					if(v>smax) smax=v;
				}
				smin/=lD; smax/=lD;
				int k=Axes[u].Base.NLayers-1;
				bool cs=false;
				for(int i=0;i<Axes[u].Base.Cut.Length;i++) {
					double p=Axes[u].Base.Cut[i];
					if(p>=smax-0.0001) continue;
					if(!cs) { k=i; cs=true; }
					if(p<=smin+0.0001) break;
				}
				F.AxisLayers[u]=k;
				rnk+=Axes[u].Base.GetRank(k);
			}

			// single sticker = full face
			F.NCutAxes=0;
			F.CutAxes=new int[0];
			F.NStickers=1;
			F.StickerMask=new byte[1,0];
			F.StickerMesh=new PMesh[1];
			PMesh stk=new PMesh(m);
			stk.Rank=rnk;
			stk.FCtr=F.Pole;
			F.StickerMesh[0]=stk;
		}
		// ═══════════════════════════════════════════════════════════════
		//  Cross-polytope Generator — face-turning version
		// ═══════════════════════════════════════════════════════════════

		private static string[] BuildCrossDef(int dim,int order) {
			var lines=new List<string>();
			lines.Add("Dim "+dim);
			lines.Add("NAxis 1");
			var sb=new StringBuilder();
			sb.Append("Faces 1");
			for(int i=1;i<dim;i++) sb.Append(",1");
			lines.Add(sb.ToString());
			sb.Length=0;
			sb.Append("Group");
			for(int k=2;k<=dim;k++) {
				sb.Append(" 1");
				for(int i=2;i<=dim;i++) sb.Append(",0");
				sb.Append("/1");
				for(int i=2;i<=dim;i++) sb.Append(i==k ? ",1" : ",0");
			}
			lines.Add(sb.ToString());
			sb.Length=0;
			sb.Append("Axis 1");
			for(int i=1;i<dim;i++) sb.Append(",1");
			lines.Add(sb.ToString());
			sb.Length=0;
			sb.Append("Twists");
			if(dim==4) {
				sb.Append(" 0,2,-1,-1/0,1,1,-2 1,-1,0,0/0,0,1,-1");
			} else {
				for(int k=2;k<dim;k++) {
					sb.Append(" 1");
					for(int p=2;p<=dim;p++) sb.Append(p==k ? ",-1" : ",0");
					sb.Append("/1");
					for(int p=2;p<=dim;p++) sb.Append(p==k+1 ? ",-1" : ",0");
				}
			}
			lines.Add(sb.ToString());
			sb.Length=0;
			if(order<=1) {
				lines.Add("Cuts");
			} else {
				sb.Append("Cuts");
				for(int i=0;i<order-1;i++) {
					double cut=1.0-2.0*(i+1)/order;
					sb.Append(" "+FormatNum(cut));
				}
				lines.Add(sb.ToString());
			}
			return lines.ToArray();
		}

		public static PuzzleStructure CreateCrossGenerated(int dim,int order) {
			string name="Cross-"+dim+"_FT_"+order;
			string[] def=BuildCrossDef(dim,order);
			var ps=new PuzzleStructure();
			ps.QGenerator="cross "+dim+" "+order;
			ps.Name=name;
			ps.Dim=dim;
			ps.FillFromStrings(def);
			ps.ExpandAxes();
			ps.ExpandFaces();
			ps.GenCrossFaces();
			ps.EnumerateStickers();
			ps.SortStickersForAxes();
			ps.CreateTwistMaps();
			return ps;
		}

		private void GenCrossFaces() {
			double R=0;
			foreach(PBaseFace F in BaseFaces) R=Math.Max(R,PGeom.VLength(F.Pole));
			int minrank=int.MaxValue;
			foreach(PBaseFace F in BaseFaces) {
				if(IsCrossSinglePiece(F)) {
					GenerateMinimalCrossFace(F,R);
					minrank=Math.Min(minrank,F.MinRank());
					continue;
				}
				CutNode face=CutNode.GenCube(Dim,R*Dim);
				double[] hpln=CutNetwork.GetPlane(F.Pole,1);
				face.Split(1,hpln,false);
				face=face.ZeroNode;
				int opgen=1;
				foreach(PFace FF in Faces) if(F.Id!=FF.Id) {
					hpln=CutNetwork.GetPlane(FF.Pole,1);
					face.Split(++opgen,hpln,true);
					if(face.Status!=CutNode.STAT_PLUS) throw new Exception("Empty face: Id="+F.Id);
				}
				int nff=face.Children.Length;
				double[][] ffpol=new double[nff][];
				for(int i=0;i<nff;i++) ffpol[i]=face.Children[i].Pole;
				F.FPoles=ffpol;
				LMesh m=new LMesh(Dim,true);
				face.FillLMesh(++opgen,m);
				m.CloseCtr();
				F.FaceMesh=new PMesh(m);
				F.FaceMesh.FCtr=F.Pole;
				double[] verts=m.pts;
				int nverts=m.npts;
				CutNetwork CN=new CutNetwork(face,Dim,opgen);
				double[][] fctrs=null;
				if(QSimplified) fctrs=CN.GetCtrs();
				F.AxisLayers=new int[Axes.Length];
				for(int u=0;u<Axes.Length;u++){
					PAxis Ax=Axes[u];
					double[] D=Ax.Dir;
					double lD=PGeom.Dist2(D,new double[Dim]);
					double smin=double.MaxValue,smax=double.MinValue;
					for(int i=0;i<nverts;i++) {
						double v=0;
						for(int j=0;j<Dim;j++) v+=D[j]*verts[i*Dim+j];
						if(v<smin) smin=v;
						if(v>smax) smax=v;
					}
					smin/=lD; smax/=lD;
					bool cs=false,cc=false;
					int k=Ax.Base.NLayers-1;
					for(int i=0;i<Ax.Base.Cut.Length;i++) {
						double p=Ax.Base.Cut[i];
						if(p>=smax-0.0001) continue;
						if(!cs) { k=i; cs=true; }
						if(p<=smin+0.0001) break;
						hpln=CutNetwork.GetPlane(Ax.Dir,p);
						CN.Split(hpln,false);
						cc=true;
					}
					if(cc) { k=-1; }
					F.AxisLayers[u]=k;
				}
				F.SetStickers(m,CN,Axes,fctrs);
				minrank=Math.Min(minrank,F.MinRank());
			}
			foreach(PBaseFace F in BaseFaces) F.SubRank(minrank);
		}

		private bool IsCrossSinglePiece(PBaseFace F) {
			double R=PGeom.VLength(F.Pole);
			foreach(PAxis Ax in Axes) {
				foreach(double cut in Ax.Base.Cut) {
					if(cut>-R+0.0001 && cut<R-0.0001) return false;
				}
			}
			return true;
		}

		private void GenerateMinimalCrossFace(PBaseFace F,double R) {
			int dim=Dim;
			double R2=R*R;
			int nverts=dim;
			LMesh m=new LMesh(Dim,true);
			int[] vIdx=new int[nverts];
			for(int i=0;i<nverts;i++) {
				double[] pt=new double[Dim];
				pt[i]=R2;
				vIdx[i]=m.AddPoint(pt,0);
			}
			for(int i=0;i<dim;i++) {
				for(int j=i+1;j<dim;j++) m.AddSeg(vIdx[i],vIdx[j]);
			}
			for(int i=0;i<dim;i++) {
				for(int j=i+1;j<dim;j++) {
					for(int k=j+1;k<dim;k++) m.AddTrg(vIdx[i],vIdx[j],vIdx[k]);
				}
			}
			m.CloseCtr();
			F.FaceMesh=new PMesh(m);
			F.FaceMesh.FCtr=F.Pole;
			F.FPoles=new double[dim][];
			for(int i=0;i<dim;i++) {
				double[] p=new double[Dim];
				p[i]=1;
				F.FPoles[i]=p;
			}
			double[] vertCoords=m.pts;
			int nVertCoords=m.npts;
			F.AxisLayers=new int[Axes.Length];
			int rnk=0;
			for(int u=0;u<Axes.Length;u++) {
				double[] D=Axes[u].Dir;
				double lD=0;
				for(int j=0;j<Dim;j++) lD+=D[j]*D[j];
				double smax=double.MinValue,smin=double.MaxValue;
				for(int i=0;i<nVertCoords;i++) {
					double v=0;
					for(int j=0;j<Dim;j++) v+=D[j]*vertCoords[i*Dim+j];
					if(v<smin) smin=v;
					if(v>smax) smax=v;
				}
				smin/=lD; smax/=lD;
				int k=Axes[u].Base.NLayers-1;
				bool cs=false;
				for(int i=0;i<Axes[u].Base.Cut.Length;i++) {
					double p=Axes[u].Base.Cut[i];
					if(p>=smax-0.0001) continue;
					if(!cs) { k=i; cs=true; }
					if(p<=smin+0.0001) break;
				}
				F.AxisLayers[u]=k;
				rnk+=Axes[u].Base.GetRank(k);
			}
			F.NCutAxes=0;
			F.CutAxes=new int[0];
			F.NStickers=1;
			F.StickerMask=new byte[1,0];
			F.StickerMesh=new PMesh[1];
			PMesh stk=new PMesh(m);
			stk.Rank=rnk;
			stk.FCtr=F.Pole;
			F.StickerMesh[0]=stk;
		}


		// ---------------------------------------------------------------
		//  Simplex Face-Turning Generator
		// ---------------------------------------------------------------

		private static string[] BuildSimplexDef(int dim,int order) {
			var lines=new List<string>();
			lines.Add("Dim "+dim);
			lines.Add("NAxis 1");
			var sb=new StringBuilder();
			sb.Append("Faces 1");
			for(int i=1;i<dim;i++) sb.Append(",1");
			lines.Add(sb.ToString());
			sb.Length=0;
			sb.Append("Group");
			double s=Math.Sqrt(dim+1);
			for(int k=3;k<=dim;k++) {
				sb.Append(" 2,-1");
				for(int i=3;i<=dim;i++) sb.Append(i==k ? ",-1" : ",0");
				sb.Append("/1,1");
				for(int i=3;i<=dim;i++) sb.Append(i==k ? ",-2" : ",0");
			}
			sb.Append(" "+(3+2*s).ToString("F12",CultureInfo.InvariantCulture));
			sb.Append(","+(-s).ToString("F12",CultureInfo.InvariantCulture));
			for(int i=3;i<=dim;i++) sb.Append(",1");
			sb.Append("/"+(3+s).ToString("F12",CultureInfo.InvariantCulture));
			sb.Append(","+(3+s).ToString("F12",CultureInfo.InvariantCulture));
			for(int i=3;i<=dim;i++) sb.Append(",2");
			lines.Add(sb.ToString());
			sb.Length=0;
			sb.Append("Axis 1");
			for(int i=1;i<dim;i++) sb.Append(",1");
			lines.Add(sb.ToString());
			sb.Length=0;
			sb.Append("Twists");
			if(dim==4) {
				// 4D simplex needs an order-2 double transposition (1 2)(3 4)
				// in addition to the order-3 face-stabilizing 3-cycles.
				sb.Append(" 2,-1,-1,0/1,1,-2,0");
				sb.Append(" 2,-1,0,-1/1,1,0,-2");
				sb.Append(" 1,-1,0,0/0,0,1,-1");
			} else {
				for(int k=3;k<=dim;k++) {
					sb.Append(" 2,-1");
					for(int i=3;i<=dim;i++) sb.Append(i==k ? ",-1" : ",0");
					sb.Append("/1,1");
					for(int i=3;i<=dim;i++) sb.Append(i==k ? ",-2" : ",0");
				}
			}
			lines.Add(sb.ToString());
			sb.Length=0;
			if(order<=1) {
				lines.Add("Cuts");
			} else {
				sb.Append("Cuts");
				for(int i=1;i<order;i++) {
					double cut=1.0-(double)i*(dim+1)/order;
					sb.Append(" "+FormatNum(cut));
				}
				lines.Add(sb.ToString());
			}
			return lines.ToArray();
		}

		public static PuzzleStructure CreateSimplexGenerated(int dim,int order) {
			string name="Simplex-"+dim+"_FT_"+order;
			string[] def=BuildSimplexDef(dim,order);
			var ps=new PuzzleStructure();
			ps.QGenerator="simplex "+dim+" "+order;
			ps.Name=name;
			ps.Dim=dim;
			ps.FillFromStrings(def);
			ps.ExpandAxes();
			ps.ExpandFaces();
			ps.GenSimplexFaces();
			ps.EnumerateStickers();
			ps.SortStickersForAxes();
			ps.CreateTwistMaps();
			return ps;
		}

		static double[] SolveLinearSystem(double[,] A,double[] b,int n) {
			double[,] aug=new double[n,n+1];
			for(int i=0;i<n;i++) {
				for(int j=0;j<n;j++) aug[i,j]=A[i,j];
				aug[i,n]=b[i];
			}
			for(int col=0;col<n;col++) {
				int pivot=col;
				for(int row=col+1;row<n;row++)
					if(Math.Abs(aug[row,col])>Math.Abs(aug[pivot,col])) pivot=row;
				if(Math.Abs(aug[pivot,col])<1e-12) return null;
				if(pivot!=col) {
					for(int j=col;j<=n;j++) {
						double tmp=aug[col,j]; aug[col,j]=aug[pivot,j]; aug[pivot,j]=tmp;
					}
				}
				for(int row=col+1;row<n;row++) {
					double factor=aug[row,col]/aug[col,col];
					for(int j=col;j<=n;j++) aug[row,j]-=factor*aug[col,j];
				}
			}
			double[] x=new double[n];
			for(int i=n-1;i>=0;i--) {
				double sum=0;
				for(int j=i+1;j<n;j++) sum+=aug[i,j]*x[j];
				if(Math.Abs(aug[i,i])<1e-12) return null;
				x[i]=(aug[i,n]-sum)/aug[i,i];
			}
			return x;
		}

		private bool IsSimplexSinglePiece(PBaseFace F) {
			// Simplex face region along the axis spans from coefficient = 1
			// (face plane) to -Dim (opposite vertex direction). A cut in
			// (-Dim, 1) subdivides the face → not single-piece.
			// Using |Pole| as threshold fails for dim≥6 where cut values
			// exceed sqrt(dim).
			foreach(PAxis Ax in Axes) {
				foreach(double cut in Ax.Base.Cut) {
					if(cut>-Dim+0.0001 && cut<1-0.0001) return false;
				}
			}
			return true;
		}

		private void GenerateMinimalSimplexFace(PBaseFace F,double R) {
			int dim=Dim;
			double R2=R*R;
			int nverts=dim;
			int baseIdx=-1;
			for(int i=0;i<Faces.Length;i++) {
				if(PGeom.VertEqual(F.Pole,Faces[i].Pole)) { baseIdx=i; break; }
			}
			if(baseIdx<0) {
				GenerateMinimalCrossFace(F,R);
				return;
			}
			int[] otherIdxs=new int[dim];
			int cnt=0;
			for(int i=0;i<Faces.Length;i++) if(i!=baseIdx) otherIdxs[cnt++]=i;
			LMesh m=new LMesh(Dim,true);
			int[] vIdx=new int[nverts];
			for(int vi=0;vi<dim;vi++) {
				double[,] mat=new double[dim,dim];
				double[] rhs=new double[dim];
				int row=0;
				for(int j=0;j<dim;j++) mat[row,j]=Faces[baseIdx].Pole[j];
				rhs[row]=R2; row++;
				for(int k=0;k<dim;k++) {
					if(k==vi) continue;
					int fidx=otherIdxs[k];
					for(int j=0;j<dim;j++) mat[row,j]=Faces[fidx].Pole[j];
					rhs[row]=R2; row++;
				}
				double[] pt=SolveLinearSystem(mat,rhs,dim);
				if(pt==null) {
					GenerateMinimalCrossFace(F,R);
					return;
				}
				vIdx[vi]=m.AddPoint(pt,0);
			}
			for(int i=0;i<dim;i++) {
				for(int j=i+1;j<dim;j++) m.AddSeg(vIdx[i],vIdx[j]);
			}
			for(int i=0;i<dim;i++) {
				for(int j=i+1;j<dim;j++) {
					for(int k=j+1;k<dim;k++) m.AddTrg(vIdx[i],vIdx[j],vIdx[k]);
				}
			}
			m.CloseCtr();
			F.FaceMesh=new PMesh(m);
			F.FaceMesh.FCtr=F.Pole;
			F.FPoles=new double[dim][];
			for(int i=0;i<dim;i++) {
				double[] p=new double[Dim];
				p[i]=1;
				F.FPoles[i]=p;
			}
			double[] vertCoords=m.pts;
			int nVertCoords=m.npts;
			F.AxisLayers=new int[Axes.Length];
			int rnk=0;
			for(int u=0;u<Axes.Length;u++) {
				double[] D=Axes[u].Dir;
				double lD=0;
				for(int j=0;j<Dim;j++) lD+=D[j]*D[j];
				double smax=double.MinValue,smin=double.MaxValue;
				for(int i=0;i<nVertCoords;i++) {
					double v=0;
					for(int j=0;j<Dim;j++) v+=D[j]*vertCoords[i*Dim+j];
					if(v<smin) smin=v;
					if(v>smax) smax=v;
				}
				smin/=lD; smax/=lD;
				int k=Axes[u].Base.NLayers-1;
				bool cs=false;
				for(int i=0;i<Axes[u].Base.Cut.Length;i++) {
					double p=Axes[u].Base.Cut[i];
					if(p>=smax-0.0001) continue;
					if(!cs) { k=i; cs=true; }
					if(p<=smin+0.0001) break;
				}
				F.AxisLayers[u]=k;
				rnk+=Axes[u].Base.GetRank(k);
			}
			F.NCutAxes=0;
			F.CutAxes=new int[0];
			F.NStickers=1;
			F.StickerMask=new byte[1,0];
			F.StickerMesh=new PMesh[1];
			PMesh stk=new PMesh(m);
			stk.Rank=rnk;
			stk.FCtr=F.Pole;
			F.StickerMesh[0]=stk;
		}

		private void GenSimplexFaces() {
			double R=0;
			foreach(PBaseFace F in BaseFaces) R=Math.Max(R,PGeom.VLength(F.Pole));
			int minrank=int.MaxValue;
			foreach(PBaseFace F in BaseFaces) {
				if(IsSimplexSinglePiece(F)) {
					GenerateMinimalSimplexFace(F,R);
					minrank=Math.Min(minrank,F.MinRank());
					continue;
				}
				CutNode face=CutNode.GenCube(Dim,R*Dim);
				double[] hpln=CutNetwork.GetPlane(F.Pole,1);
				face.Split(1,hpln,false);
				face=face.ZeroNode;
				int opgen=1;
				foreach(PFace FF in Faces) if(F.Id!=FF.Id) {
					hpln=CutNetwork.GetPlane(FF.Pole,1);
					face.Split(++opgen,hpln,true);
					if(face.Status!=CutNode.STAT_PLUS) throw new Exception("Empty face: Id="+F.Id);
				}
				int nff=face.Children.Length;
				double[][] ffpol=new double[nff][];
				for(int i=0;i<nff;i++) ffpol[i]=face.Children[i].Pole;
				F.FPoles=ffpol;
				LMesh m=new LMesh(Dim,true);
				face.FillLMesh(++opgen,m);
				m.CloseCtr();
				F.FaceMesh=new PMesh(m);
				F.FaceMesh.FCtr=F.Pole;
				double[] verts=m.pts;
				int nverts=m.npts;
				CutNetwork CN=new CutNetwork(face,Dim,opgen);
				double[][] fctrs=null;
				if(QSimplified) fctrs=CN.GetCtrs();
				F.AxisLayers=new int[Axes.Length];
				for(int u=0;u<Axes.Length;u++){
					PAxis Ax=Axes[u];
					double[] D=Ax.Dir;
					double lD=PGeom.Dist2(D,new double[Dim]);
					double smin=double.MaxValue,smax=double.MinValue;
					for(int i=0;i<nverts;i++) {
						double v=0;
						for(int j=0;j<Dim;j++) v+=D[j]*verts[i*Dim+j];
						if(v<smin) smin=v;
						if(v>smax) smax=v;
					}
					smin/=lD; smax/=lD;
					bool cs=false,cc=false;
					int k=Ax.Base.NLayers-1;
					for(int i=0;i<Ax.Base.Cut.Length;i++) {
						double p=Ax.Base.Cut[i];
						if(p>=smax-0.0001) continue;
						if(!cs) { k=i; cs=true; }
						if(p<=smin+0.0001) break;
						hpln=CutNetwork.GetPlane(Ax.Dir,p);
						CN.Split(hpln,false);
						cc=true;
					}
					if(cc) { k=-1; }
					F.AxisLayers[u]=k;
				}
				F.SetStickers(m,CN,Axes,fctrs);
				minrank=Math.Min(minrank,F.MinRank());
			}
			foreach(PBaseFace F in BaseFaces) F.SubRank(minrank);
		}
	}

	class PermByMatr {
		internal int[] CvAxes;
		internal int[] CvFaces;
		internal int[] PermAxes,StkMask;
		internal int[] InvAxes;

		internal PermByMatr(int nax,int nfc) {
			CvAxes=new int[nax];
			CvFaces=new int[nfc];
			PermAxes=new int[100];
			StkMask=new int[100];
			InvAxes=new int[100];
		}
		internal void ReallocPerm(int lp) {
			if(lp<=PermAxes.Length) return;
			PermAxes=new int[2*lp];
			StkMask=new int[2*lp];
			InvAxes=new int[2*lp];
		}
	}

}
