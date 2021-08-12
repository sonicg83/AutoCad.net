// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections;
using System.IO;
using Autodesk.AutoCAD.EditorInput;
using System.Linq;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCad_Utility.BindXrefs))]
[assembly: CommandClass(typeof(AutoCad_Utility.FieldToTextclass))]
[assembly: CommandClass(typeof(AutoCad_Utility.GetLayoutHandles))]
[assembly: CommandClass(typeof(AutoCad_Utility.Ainsert))]


namespace AutoCad_Utility
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class BindXrefs
    {
        [CommandMethod("BindXrefs")]
        public void BindAllXrefs() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            try
            {
                using (Transaction Tx = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)Tx.GetObject(db.BlockTableId, OpenMode.ForRead);
                    ObjectIdCollection BindobjectIDs = new ObjectIdCollection();
                    ObjectIdCollection detachobjectIDs = new ObjectIdCollection();
                    ArrayList bindxrefnames = new ArrayList();
                    ArrayList detachxrefnames = new ArrayList();
                    foreach (ObjectId id in bt)
                    {
                        BlockTableRecord btr = (BlockTableRecord)Tx.GetObject(id, OpenMode.ForRead);
                        if (btr.IsFromExternalReference == true && btr.IsResolved == true)
                        {
                            BindobjectIDs.Add(id);
                            bindxrefnames.Add(btr.PathName);
                        }
                        if (btr.IsFromExternalReference == true && btr.IsUnloaded == true)
                        {
                            detachobjectIDs.Add(id);
                            detachxrefnames.Add(btr.PathName);
                        }
                    }
                    if (BindobjectIDs.Count > 0)
                    {
                        db.BindXrefs(BindobjectIDs, false);
                        ed.WriteMessage("\n绑定了{0}个外部参照，以下为外部参照名：", BindobjectIDs.Count);
                        if (bindxrefnames.Count > 0)
                        {
                            foreach (string name in bindxrefnames)
                            {
                                ed.WriteMessage("\n{0}", name);
                            }
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n未绑定任何外部参照");
                    }
                    if (detachobjectIDs.Count > 0)
                    {
                        foreach (ObjectId id in detachobjectIDs)
                        {
                            db.DetachXref(id);
                        }
                        ed.WriteMessage("\n拆离了{0}个外部参照，以下为外部参照名：", detachobjectIDs.Count);
                        foreach (string name in detachxrefnames)
                        {
                            ed.WriteMessage("\n{0}", name);
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n未拆离任何外部参照");
                    }

                    Tx.Commit();
                }
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog("Error: " + ex.Message);
            }

        }

    }
    public class FieldToTextclass
    {
        //获取当前文档编辑器
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //获取当前文档数据库
        Database db = Application.DocumentManager.MdiActiveDocument.Database;

        //根据选择转换
        [CommandMethod("FTT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public void FieldtoText()
        {
            //获取pickfirst选择集
            PromptSelectionResult SelResult = null;
            SelResult = ed.SelectImplied();
            //判断是否存在pickfirst选择集
            if (SelResult.Status == PromptStatus.OK)
            {
                //存在pickfirst选择集，进行转换
                convertToText(SelResult);
            }
            //如果没有pickfirst选择集，则提示用户选择文字
            else
            {
                //创建选择集过滤规则数组
                TypedValue[] FilterRule = new TypedValue[]
              {
                  new TypedValue((int)DxfCode.Operator,"<or"),
                  //注意下面，创建选择过滤规则时单行文字是“Text”，不是“DBText”！另外块参照是INSERT
                  new TypedValue((int)DxfCode.Start,"Text"),
                  new TypedValue((int)DxfCode.Start,"MText"),
                  new TypedValue((int)DxfCode.Start,"INSERT"),
                  new TypedValue((int)DxfCode.Operator,"or>"),
              };
                //利用过滤规则创建选择集过滤器实例
                SelectionFilter SelFilter = new SelectionFilter(FilterRule);
                //创建选择选项
                PromptSelectionOptions SelOpts = new PromptSelectionOptions();
                SelOpts.MessageForAdding = "选择单行文字、多行文字或属性块";
                //进行用户选择
                SelResult = ed.GetSelection(SelOpts, SelFilter);
                //判断选择集是否正常
                //注：这个很重要，没有的话程序中断会直接非法操作退出CAD！
                if (SelResult.Status == PromptStatus.OK)
                {
                    //获取选择集正常，进行转换
                    convertToText(SelResult);
                }
            }
        }

        //全部转换
        [CommandMethod("FTTA", CommandFlags.Modal)]
        public void FieldToTextAll()
        {
            TypedValue[] FilterRule = new TypedValue[]
              {
                  new TypedValue((int)DxfCode.Operator,"<or"),
                  new TypedValue((int)DxfCode.Start,"Text"),
                  new TypedValue((int)DxfCode.Start,"MText"),
                  new TypedValue((int)DxfCode.Start,"INSERT"),
                  new TypedValue((int)DxfCode.Operator,"or>"),
              };

            SelectionFilter SelFilter = new SelectionFilter(FilterRule);

            PromptSelectionResult SelResult = ed.SelectAll(SelFilter);

            if (SelResult.Status == PromptStatus.OK)
            {
                //获取选择集正常，进行转换
                convertToText(SelResult);
            }

        }

        //这是转换操作函数
        private void convertToText(PromptSelectionResult SelResult)
        {
            //初始化转换计数
            int count = 0;
            //获取选择集所有实体的ID
            ObjectId[] ObIDs = SelResult.Value.GetObjectIds();
            //启动事务进行实体操作
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //加一个try进行异常处理
                try
                {
                    //遍历选择集中的所有实体
                    foreach (ObjectId ObID in ObIDs)
                    {
                        //依据实体ID获取实体对象
                        //为加快系统处理速度，OpenMode使用ForRead，轮到需要的实体时再用UpgradeOpen()改为ForWrite
                        DBObject ent = trans.GetObject(ObID, OpenMode.ForRead) as DBObject;
                        //属性块的情况麻烦些，单独拎出来                  
                        if (ent != null)
                        {
                            if (ent.GetType().Name == "BlockReference")
                            {
                                BlockReference blk = ent as BlockReference;
                                //判断是否是动态块
                                if (blk.IsDynamicBlock)
                                {
                                    AttributeCollection ac = blk.AttributeCollection;
                                    foreach (ObjectId arID in ac)
                                    {
                                        AttributeReference ar = arID.GetObject(OpenMode.ForWrite) as AttributeReference;
                                        if (ar.HasFields)
                                        {
                                            ar.ConvertFieldToText();
                                            count++;
                                        }
                                    }
                                }
                            }
                            if (ent.HasFields)
                            {
                                //获取实体的类型进行判断
                                switch (ent.GetType().Name)
                                {
                                    //单行文字
                                    case "DBText":
                                        {
                                            DBText Dt = (DBText)ent;
                                            //改为ForWrite
                                            Dt.UpgradeOpen();
                                            //利用DBText的ConvertFieldToText()方法转换
                                            //注：该方法用在不含有字段的文字时会报错
                                            Dt.ConvertFieldToText();
                                            count++;
                                            break;
                                        }
                                    //多行文字
                                    case "MText":
                                        {
                                            MText Dt = (MText)ent;
                                            Dt.UpgradeOpen();
                                            Dt.ConvertFieldToText();
                                            count++;
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    //提交事务保存
                    trans.Commit();
                }

                catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                {
                    ed.WriteMessage("\n出错了！" + Ex.ToString());
                }
                finally
                {
                    //关闭事务
                    trans.Dispose();
                }
            }
            ed.WriteMessage("\n完成转换" + count + "个字段");
        }
    }

    public class GetLayoutHandles
    {
        [CommandMethod("GetLayoutHandles")]
        public void ListLayouts()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            ArrayList LayoutList = new ArrayList();
            // Get the layout dictionary of the current database
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                DBDictionary lays =
                    acTrans.GetObject(acCurDb.LayoutDictionaryId,
                        OpenMode.ForRead) as DBDictionary;

                // Step through and list each named layout except Model
                foreach (DBDictionaryEntry item in lays)
                {
                    ObjectId id = item.Value;
                    Handle handle = id.Handle;
                    Hashtable Layout = new Hashtable
                    {
                        { "name", item.Key },
                        { "handle", handle.ToString() }
                    };
                    if ((string)Layout["name"] != "Model")
                    {
                        LayoutList.Add(Layout);
                    }
                }

                // Abort the changes to the database
                acTrans.Abort();
            }
            //测试用代码
            /*acDoc.Editor.WriteMessage("\nLayouts:");
            foreach (Hashtable layout in LayoutList)
            {
                acDoc.Editor.WriteMessage("\n" +layout["name"]+"的句柄："+layout["handle"]);
            }
            */
            string dwgfile = acCurDb.Filename;
            string filepath = string.Format("{0}\\{1}.txt", Path.GetDirectoryName(dwgfile),Path.GetFileNameWithoutExtension(dwgfile));
            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                foreach (Hashtable layout in LayoutList)
                {
                    sw.WriteLine(layout["name"] + "=" + layout["handle"]);
                }
                sw.Flush();
                sw.Close();

            }
        }
    }
    public class Ainsert
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
                        if(!File.Exists(filename))
                        {
                            ed.WriteMessage("\n文件路径无效！");
                            return;
                        }
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
                    foreach (ObjectId id in bt)
                    {
                        BlockTableRecord btr = Trans.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                        if (btr.IsFromExternalReference)
                        {
                            RecordList.Add(btr);
                        }
                        if (btr.IsLayout == false && btr.IsFromExternalReference == false)
                        {
                            BlockList.Add(btr);
                        }

                    }
                    if (RecordList.Count == 0)
                    {
                        ed.WriteMessage("\n图形中未找到任何外部参照！");
                        return;
                    }
                    var query = from BlockTableRecord record in RecordList
                                where record.Name == xrefname
                                select record;
                    if (!query.Any())
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

                    foreach (BlockTableRecord blockrecord in BlockList)
                    {
                        foreach (ObjectId id in blockrecord)
                        {
                            RXClass entityclass = id.ObjectClass;
                            if (entityclass.Name == "AcDbBlockReference")
                            {
                                BlockReference block = Trans.GetObject(id, OpenMode.ForWrite) as BlockReference;
                                if (block.Name == xrefname)
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

        [CommandMethod("dellayouts")]
        public void DelLayouts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            LayoutManager LMR = LayoutManager.Current;
            ArrayList Layoutlist = new ArrayList();
            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取布局列表(剔除模型空间)
                    DBDictionary Layouts = Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;

                    foreach (DBDictionaryEntry item in Layouts)
                    {
                        if (item.Key != "Model")
                        {
                            Layout layoutobject = Trans.GetObject(item.Value, OpenMode.ForRead) as Layout;
                            Layoutlist.Add(layoutobject);
                        }
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
            foreach(Layout LT in Layoutlist)
            {
                LMR.DeleteLayout(LT.LayoutName);
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
