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
[assembly: CommandClass(typeof(SetDataLink.MyCommands))]

namespace SetDataLink
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
        [CommandMethod("SetDataLink")]
        public void SetDataLink() // This method can have any name
        {
            // Put your command code here
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //获取目录表格的容量
            int NumberPerPage = 1;
            PromptIntegerOptions GetNumberOption = new PromptIntegerOptions("\n输入每张目录的最大容量：");
            GetNumberOption.AllowNegative = false;
            GetNumberOption.AllowZero = false;
            PromptIntegerResult GetNumberResult = ed.GetInteger(GetNumberOption);
            if(GetNumberResult.Status == PromptStatus.OK)
            {
                NumberPerPage = GetNumberResult.Value;
            }
            else
            {
                return;
            }
            //获取图纸目录数据文件
            string DataFile = "";
            PromptFileNameResult DataFileResult = ed.GetFileNameForOpen("\n输入图纸目录数据文件路径：");
            if(DataFileResult.Status == PromptStatus.OK)
            {
                DataFile = DataFileResult.StringResult;
            }
            else
            {
                return;
            }
            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                DBDictionary Layouts = Trans.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
                ArrayList Layoutlist = new ArrayList();
                foreach (DBDictionaryEntry item in Layouts)
                {
                    if (item.Key != "Model")
                    {
                        Layoutlist.Add(item.Key);
                    }
                }
                int NumberOfList = Layoutlist.Count;
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
                ed.WriteMessage("\nLayout:{0}", Layoutlist.Count);
                ed.WriteMessage("\nTables:{0}", TableIDs.Count);
                DataLinkManager dlm = db.DataLinkManager;
                NumberOfList = 1;
                for (int i = 0; i < NumberOfList; i++)
                {
                    DataLink dl = new DataLink();
                    dl.DataAdapterId = "AcExcel";
                    dl.Name = "图纸目录" + (i + 1).ToString();
                    dl.Description ="图纸目录数据链接" + (i + 1).ToString();
                    string location = string.Format("!图纸目录!A{0}:E{1}", (2 + i * NumberPerPage), ((1 + i) * NumberPerPage) + 1);
                    dl.ConnectionString = DataFile + location;
                    dl.DataLinkOption = DataLinkOption.PersistCache;
                    dl.UpdateOption |= (int)UpdateOption.AllowSourceUpdate | (int)UpdateOption.SkipFormat;
                    ObjectId dlId = dlm.AddDataLink(dl);                    
                    //ed.WriteMessage("\n链接字符串:{0}", dl.ConnectionString);
                    Trans.AddNewlyCreatedDBObject(dl, true);
                    Table tb = (Table)Trans.GetObject((ObjectId)TableIDs[i], OpenMode.ForWrite);
                    tb.Cells[1, 0].DataLink = dlId;
                    tb.GenerateLayout();
                }
                Trans.Commit();
            }            
        }      
    }
}
