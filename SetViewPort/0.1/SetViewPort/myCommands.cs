// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SetViewPort.MyCommands))]

namespace SetViewPort
{
    public class MyCommands
    {
        //增加方向参数，向右为加，向左为减
        private Viewport GetViewport(ViewTableRecord InputView, Point2d BasePoint, double Scale,bool ToRight)
        {
            Viewport NewViewport = new Viewport();
            if(ToRight)
            {
                NewViewport.CenterPoint = new Point3d(BasePoint.X + (InputView.Width / 2) * Scale, BasePoint.Y + (InputView.Height / 2) * Scale, 0);
            }
            else
            {
                NewViewport.CenterPoint = new Point3d(BasePoint.X - (InputView.Width / 2) * Scale, BasePoint.Y - (InputView.Height / 2) * Scale, 0);
            }
            
            NewViewport.Height = InputView.Height * Scale;
            NewViewport.Width = InputView.Width * Scale;
            NewViewport.ViewCenter = InputView.CenterPoint;
            NewViewport.ViewDirection = InputView.ViewDirection;
            NewViewport.ViewHeight = InputView.Height;
            NewViewport.ViewTarget = InputView.Target;
            NewViewport.TwistAngle = InputView.ViewTwist;
            return NewViewport;
        }

        // Modal Command with localized name
        [CommandMethod("SetViewPort")]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            LayerStateManager layerState = db.LayerStateManager;
            string ClipLayerName = "TK-视口";

            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取布局列表(剔除模型空间)
                    DBDictionary Layouts = Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                    ArrayList Layoutlist = new ArrayList();
                    foreach (DBDictionaryEntry item in Layouts)
                    {
                        if (item.Key != "Model")
                        {
                            Layout layoutobject = Trans.GetObject(item.Value, OpenMode.ForRead) as Layout;
                            Layoutlist.Add(layoutobject);
                        }
                    }
                    //获取view列表,注意哦，是symbotable，不能用viewtable，会闪退
                    SymbolTable MyVt = Trans.GetObject(db.ViewTableId, OpenMode.ForRead) as SymbolTable;
                    ArrayList viewlist = new ArrayList();
                    foreach (ObjectId viewID in MyVt)
                    {
                        ViewTableRecord VR = Trans.GetObject(viewID, OpenMode.ForRead) as ViewTableRecord;
                        viewlist.Add(VR);
                    }
                    if(viewlist.Count == 0)
                    {
                        ed.WriteMessage("\n未发现存储的视图！");
                        return;
                    }

                    double scale = 1;
                    PromptDoubleOptions GetNumberOption = new PromptDoubleOptions("\n输入视口比例，默认为1");
                    GetNumberOption.AllowNegative = false;
                    GetNumberOption.AllowZero = false;
                    GetNumberOption.AllowNone = true;
                    PromptDoubleResult GetNumberResult = ed.GetDouble(GetNumberOption);
                    if (GetNumberResult.Status == PromptStatus.OK)
                    {
                        scale = GetNumberResult.Value;
                    }
                    else
                    {
                        return;
                    }

                    for (int i = 0; i < Layoutlist.Count; i++)
                    {
                        if (i == viewlist.Count)
                        {
                            break;
                        }
                        Layout LT = Layoutlist[i] as Layout;
                        string[] split = LT.LayoutName.Split(' ');
                        Regex patten = new Regex(@"^0*\d{1,}");
                        Regex replace = new Regex("^0*");
                        string match = replace.Replace(split[0], "");                        
                        var query = from ViewTableRecord view in viewlist
                                    where replace.Replace(view.Name, "") == match
                                    select view;
                        if(!query.Any())
                        {
                            ed.WriteMessage("\n布局“{0}”未找到匹配的视图，没有成功生成视口！", LT.LayoutName);
                            i--;
                            continue;
                        }
                        
                        ViewTableRecord VR = query.First() ;
                        Viewport VP = GetViewport(VR, new Point2d(0, 0), scale, false);

                        BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                        BTR.AppendEntity(VP);
                        Trans.AddNewlyCreatedDBObject(VP, true);

                        LayoutManager.Current.SetCurrentLayoutId(LT.Id);
                        VP.On = true;
                        //恢复视图的图层状态
                       // ed.WriteMessage("\n<" + VR.LayerState + ">");
                        if(VR.LayerState != "")
                        {
                            layerState.RestoreLayerState(VR.LayerState, VP.Id, 1, LayerStateMasks.CurrentViewport);
                        }
                        
                        //开始选择多段线裁剪视口
                        TypedValue[] Filter = new TypedValue[]
                       {
                            new TypedValue((int)DxfCode.Operator,"<and"),
                            new TypedValue((int)DxfCode.LayoutName,LT.LayoutName),
                            new TypedValue((int)DxfCode.LayerName,ClipLayerName),
                            new TypedValue((int)DxfCode.Operator,"<or"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE3D"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE2D"),
                            new TypedValue((int)DxfCode.Start,"LWPOLYLINE"),
                            new TypedValue((int)DxfCode.Operator,"or>"),
                            new TypedValue((int)DxfCode.Operator,"and>")
                       };
                        PromptSelectionResult selresult = ed.SelectAll(new SelectionFilter(Filter));
                        if (selresult.Status == PromptStatus.OK)
                        {
                            ObjectId[] IDs = selresult.Value.GetObjectIds();
                            VP.NonRectClipEntityId = IDs[0];
                            VP.NonRectClipOn = true;
                            //ed.WriteMessage("\n呵呵，剪裁了视口哦");
                        }
                        else
                        {
                            ed.WriteMessage("\n布局“{0}”中未找到裁剪视口的多义线！视口裁剪失败！", LT.LayoutName);
                        }

                    }

                    Trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                {
                    ed.WriteMessage("\n出错啦！{0}", Ex.ToString());
                }
                finally
                {
                    Trans.Dispose();

                }

            }
        }
    }

}
