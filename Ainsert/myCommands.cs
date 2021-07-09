// (C) Copyright 2021 by  
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections;
using System.IO;
using System.Linq;

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

                    
                    foreach (Layout LT in Layoutlist)
                    {
                        ObjectId xrefID = db.AttachXref(filename, Path.GetFileNameWithoutExtension(filename));
                        if (!xrefID.IsNull)
                        {
                            BlockReference xref = new BlockReference(insertpoint, xrefID);
                            BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                            BTR.AppendEntity(xref);
                            Trans.AddNewlyCreatedDBObject(xref, true);                           
                        }

                        //Layout LT = Layoutlist[0] as Layout;
                        //BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                        //BTR.AppendEntity(xref);
                        // Trans.AddNewlyCreatedDBObject(xref,true);
                    }


                    Trans.Commit();
                }
                catch (Exception Ex)
                {
                    ed.WriteMessage("出错啦！{0}", Ex.ToString());
                }
                finally
                {
                    Trans.Dispose();

                }

            }
        }

        [CommandMethod("clearxref")]
        public void MyCommand2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptStringOptions stringoptions = new PromptStringOptions("\n输入要拆除的外部参照名");
            PromptResult stringresult = ed.GetString(stringoptions);
            string xrefname = "";
            if (stringresult.Status == PromptStatus.OK)
            {
                xrefname = stringresult.StringResult;
            }
            else
            {
                return;
            }

            TypedValue[] FilterRule = new TypedValue[]
                      {
                        new TypedValue((int)DxfCode.Operator,"<and"),
                        new TypedValue((int)DxfCode.Start,"INSERT"),
                        new TypedValue((int)DxfCode.BlockName,xrefname),
                        new TypedValue((int)DxfCode.Operator,"and>"),
                      };

            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = Trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    ArrayList RecordList = new ArrayList();
                    ArrayList BlockList = new ArrayList();
                    foreach(ObjectId id in bt)
                    {
                        BlockTableRecord btr = Trans.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                        if(btr.IsFromExternalReference)
                        {
                            RecordList.Add(btr);
                        }
                        if(btr.IsLayout == false && btr.IsFromExternalReference == false)
                        {
                            BlockList.Add(btr);
                        }

                    }
                    if(RecordList.Count == 0)
                    {
                        ed.WriteMessage("\n图形中未找到任何外部参照！");
                        return;
                    }
                    var query = from BlockTableRecord record in RecordList
                                where record.Name == xrefname
                                select record;
                    if(!query.Any())
                    {
                        ed.WriteMessage("\n未找到名为 {0} 的外部参照！", xrefname);
                        return;
                    }
                    BlockTableRecord Btr = query.First();         

                    PromptSelectionResult XrefSelection = ed.SelectAll(new SelectionFilter(FilterRule));
                    if (XrefSelection.Status == PromptStatus.OK)
                    {
                        ObjectId[] ids = XrefSelection.Value.GetObjectIds();
                        foreach (ObjectId ID in ids)
                        {
                            BlockReference xref = Trans.GetObject(ID, OpenMode.ForWrite) as BlockReference;
                            xref.Erase();
                        }
                    }

                    foreach(BlockTableRecord blockrecord in BlockList)
                    {
                        foreach(ObjectId id in blockrecord)
                        {
                            RXClass entityclass = id.ObjectClass;
                            if(entityclass.Name == "AcDbBlockReference")
                            {
                                BlockReference block = Trans.GetObject(id, OpenMode.ForWrite) as BlockReference;
                                if(block.Name == xrefname)
                                {
                                    block.Erase();
                                }
                            }
                        }
                    }

                    db.DetachXref(Btr.Id);
                    /*
                    DBDictionary Layouts = Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                    ArrayList Layoutlist = new ArrayList();
                    foreach (DBDictionaryEntry item in Layouts)
                    {
                        Layoutlist.Add(item.Key);
                    }
                    foreach (string name in Layoutlist)
                    {
                        TypedValue[] FilterRule = new TypedValue[]
                       {
                        new TypedValue((int)DxfCode.Operator,"<and"),
                        new TypedValue((int)DxfCode.Start,"INSERT"),
                        new TypedValue((int)DxfCode.BlockName,xrefname),
                        new TypedValue((int)DxfCode.LayoutName,name),
                        new TypedValue((int)DxfCode.Operator,"and>"),
                       };
                        PromptSelectionResult XrefSelection = ed.SelectAll(new SelectionFilter(FilterRule));
                        if(XrefSelection.Status == PromptStatus.OK)
                        {
                            ObjectId[] ids = XrefSelection.Value.GetObjectIds();
                            foreach(ObjectId ID in ids)
                            {
                                BlockReference xref = Trans.GetObject(ID, OpenMode.ForWrite) as BlockReference;
                                xref.Erase();
                            }
                        }
                    }
                    */


                    Trans.Commit();
                }
                catch (Exception Ex)
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
        [CommandMethod("restart")]
        public void TestCommand() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;
            ed.WriteMessage("\n中断了哦");

        }
        */
    }

}
