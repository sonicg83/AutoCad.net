// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Collections;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SetDataLink.DataLink))]

namespace SetDataLink
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class DataLink
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
        [CommandMethod("SetDataLink")]
        public void SetDataLink() // This method can have any name
        {
            // Put your command code here
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //获取目录表格的容量
            int NumberPerPage = 1;
            PromptIntegerOptions GetNumberOption = new PromptIntegerOptions("\n输入每张表格的最大容量");
            GetNumberOption.AllowNegative = false;
            GetNumberOption.AllowZero = false;
            PromptIntegerResult GetNumberResult = ed.GetInteger(GetNumberOption);
            if (GetNumberResult.Status == PromptStatus.OK)
            {
                NumberPerPage = GetNumberResult.Value;
            }
            else
            {
                return;
            }
            //获取图纸目录数据文件
            string DataFile = "";
            PromptOpenFileOptions fileoption = new PromptOpenFileOptions("\n输入链接数据文件路径");
            fileoption.InitialDirectory = System.IO.Path.GetDirectoryName(db.Filename);
            fileoption.Filter = "Excel Documents (*.xlsx) |*.xlsx";
            PromptFileNameResult DataFileResult = ed.GetFileNameForOpen(fileoption);
            if (DataFileResult.Status == PromptStatus.OK)
            {
                DataFile = DataFileResult.StringResult;
            }
            else
            {
                return;
            }
            //获取数据表范围信息
            string SheetName = "图纸目录";
            string StartCol = "A";
            string EndCol = "E";
            int StartRow = 2;
            PromptResult GetSheetName = ed.GetString("\n输入链接数据表名称");
            if (GetSheetName.Status == PromptStatus.OK)
            {
                SheetName = GetSheetName.StringResult;
            }
            else
            {
                return;
            }
            PromptResult GetStartCol = ed.GetString("\n输入数据起始列");
            if (GetStartCol.Status == PromptStatus.OK)
            {
                StartCol = GetStartCol.StringResult;
            }
            else
            {
                return;
            }
            PromptResult GetEndCol = ed.GetString("\n输入数据结束列");
            if (GetEndCol.Status == PromptStatus.OK)
            {
                EndCol = GetEndCol.StringResult;
            }
            else
            {
                return;
            }
            PromptIntegerResult GetStartRow = ed.GetInteger("\n输入数据起始行");
            if (GetStartRow.Status == PromptStatus.OK)
            {
                StartRow = GetStartRow.Value;
            }
            else
            {
                return;
            }
            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                DBDictionary Layouts = Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                ArrayList Layoutlist = new ArrayList();
                foreach (DBDictionaryEntry item in Layouts)
                {
                    if (item.Key != "Model")
                    {
                        Layoutlist.Add(item.Key);
                    }
                }
                //int NumberOfList = Layoutlist.Count;
                ArrayList TableIDs = new ArrayList();
                foreach (string name in Layoutlist)
                {
                    TypedValue[] Filter = new TypedValue[]
                        {
                            new TypedValue((int)DxfCode.Operator,"<and"),
                            new TypedValue((int)DxfCode.LayoutName,name),
                            new TypedValue((int)DxfCode.Start,"ACAD_TABLE"),
                            new TypedValue((int)DxfCode.Operator,"and>"),
                        };
                    PromptSelectionResult selresult = ed.SelectAll(new SelectionFilter(Filter));
                    if (selresult.Status == PromptStatus.OK)
                    {
                        ObjectId[] ids = selresult.Value.GetObjectIds();
                        TableIDs.Add(ids[0]);
                    }
                }
                int NumberOfTables = TableIDs.Count;
                /*
                ed.WriteMessage("\nLayout:{0}", Layoutlist.Count);
                foreach(string name in Layoutlist)
                {
                    ed.WriteMessage("\nLayoutname:{0}", name);
                }
                ed.WriteMessage("\nTables:{0}", TableIDs.Count);
                */
                DataLinkManager dlm = db.DataLinkManager;
                try
                {
                    for (int i = 0; i < NumberOfTables; i++)
                    {
                        Autodesk.AutoCAD.DatabaseServices.DataLink dl = new Autodesk.AutoCAD.DatabaseServices.DataLink();
                        dl.DataAdapterId = "AcExcel";
                        dl.Name = SheetName + (i + 1).ToString();
                        dl.Description = SheetName + "数据链接" + (i + 1).ToString();
                        string location = string.Format("!{0}!{1}{2}:{3}{4}", SheetName, StartCol, (StartRow + i * NumberPerPage), EndCol, ((1 + i) * NumberPerPage) + StartRow - 1);
                        dl.ConnectionString = DataFile + location;
                        dl.DataLinkOption = DataLinkOption.PersistCache;
                        dl.UpdateOption |= (int)UpdateOption.AllowSourceUpdate | (int)UpdateOption.SkipFormat;
                        ObjectId dlId = dlm.AddDataLink(dl);
                        ed.WriteMessage("\n链接字符串:{0}", dl.ConnectionString);
                        Trans.AddNewlyCreatedDBObject(dl, true);
                        Table tb = (Table)Trans.GetObject((ObjectId)TableIDs[i], OpenMode.ForWrite);
                        tb.Cells[1, 0].DataLink = dlId;
                        tb.GenerateLayout();
                    }
                    Trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                {
                    ed.WriteMessage("\n出错啦！" + Ex.ToString());
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
