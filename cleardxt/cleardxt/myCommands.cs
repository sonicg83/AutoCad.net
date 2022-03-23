// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(cleardxt.MyCommands))]

namespace cleardxt
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        // Modal Command with localized name
        [CommandMethod("cleardxt")]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    TypedValue[] Filter = new TypedValue[]
                       {             
                           new TypedValue((int)DxfCode.Operator,"<and"),
                           new TypedValue((int)DxfCode.LayoutName,"Model"),
                            new TypedValue((int)DxfCode.Operator,"<or"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE3D"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE2D"),
                            new TypedValue((int)DxfCode.Start,"LWPOLYLINE"),
                            new TypedValue((int)DxfCode.Operator,"or>"),
                            new TypedValue((int)DxfCode.Operator,"and>"),
                       };
                    PromptSelectionResult selresult = ed.SelectAll(new SelectionFilter(Filter));
                    ObjectId[] Ids; 
                    if (selresult.Status == PromptStatus.OK)
                    {
                        Ids = selresult.Value.GetObjectIds();
                    }
                    else
                    {
                        ed.WriteMessage("\n未找到任何多段线！");
                        return;
                    }
                    #region testcode

                    ed.WriteMessage("\n搜索到>{0}<个对象！", selresult.Value.Count);
                    #endregion
                    
                    int counter = 0;
                    foreach(ObjectId Id in Ids)
                    {
                        Curve cur = Trans.GetObject(Id, OpenMode.ForRead) as Curve ;
                        switch(cur.GetType().Name)
                        {
                            case "Polyline":
                                {
                                    Polyline PL = cur as Polyline;
                                    if(PL.Length == 0)
                                    {
                                        break;
                                    }
                                    if(PL.HasWidth)
                                    {
                                        PL.UpgradeOpen();
                                        for(int i =0;i < PL.NumberOfVertices;i++)
                                        {
                                            PL.SetStartWidthAt(i, 0);
                                            PL.SetEndWidthAt(i, 0);
                                        }
                                        counter++;
                                    }
                                    break;
                                }
                            case "Polyline2d":
                                {
                                    Polyline2d PL2d = cur as Polyline2d;
                                    if(PL2d.Length == 0)
                                    {
                                        break;
                                    }
                                    if(PL2d.DefaultEndWidth !=0 || PL2d.DefaultStartWidth !=0)
                                    {
                                        PL2d.UpgradeOpen();
                                        PL2d.ConstantWidth = 0;
                                        counter++;
                                    }
                                    break;
                                }                           
                        }
                    }
                    ed.WriteMessage("\n共将{0}条多段线的全局宽度调整为0.", counter);
                    
                    Trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                {
                    ed.WriteMessage("出错啦！{0}", Ex.ToString());
                }
                finally
                {
                    Trans.Dispose();

                }
            }
        }
        /*
        [CommandMethod("abort")]
        public void MyCommand2() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            ed.WriteMessage("\n中断啦！");
        }
        */
        }

}
