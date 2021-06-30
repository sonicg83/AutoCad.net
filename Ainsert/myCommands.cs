// (C) Copyright 2021 by  
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections;
using System.IO;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Ainsert.MyCommands))]

namespace Ainsert
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
        [CommandMethod("Ainsert")]
        public void MyCommand() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    PromptOpenFileOptions fileoptions = new PromptOpenFileOptions("\n输入要插入的图形文件");
                    fileoptions.Filter = "dwg图形文件(*.dwg)|*.dwg";
                    string filename = "";
                    PromptFileNameResult fileresult = ed.GetFileNameForOpen(fileoptions);
                    if (fileresult.Status == PromptStatus.OK)
                    {
                        filename = fileresult.StringResult;
                    }
                    else
                    {
                        return;
                    }
                    PromptPointResult pointResult = ed.GetPoint("\n输入插入点");
                    Point3d insertpoint = new Point3d(0, 0, 0);
                    if (pointResult.Status == PromptStatus.OK)
                    {
                        insertpoint = pointResult.Value;
                    }
                    else
                    {
                        return;
                    }
                   
                        
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

                        int i = 0;
                    foreach (Layout LT in Layoutlist)
                    {
                        ObjectId xrefID = db.AttachXref(filename, Path.GetFileNameWithoutExtension(filename));
                        if (!xrefID.IsNull)
                        {
                            BlockReference xref = new BlockReference(insertpoint, xrefID);
                            BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                            BTR.AppendEntity(xref);
                            Trans.AddNewlyCreatedDBObject(xref, true);
                            i++;
                            xref.Erase();
                        }

                        //Layout LT = Layoutlist[0] as Layout;
                        //BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                        //BTR.AppendEntity(xref);
                        // Trans.AddNewlyCreatedDBObject(xref,true);
                    }
                    

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
        [CommandMethod("restart")]
        public void TestCommand() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;
            ed.WriteMessage("\n中断了哦");

        }
    }

}
