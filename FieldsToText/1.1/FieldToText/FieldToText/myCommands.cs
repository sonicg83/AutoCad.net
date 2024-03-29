﻿// (C) Copyright 2021 by  
//1.1版，增加转换属性块中的字段
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(FieldToText.MyCommands))]

namespace FieldToText
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands
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
                                    foreach(ObjectId arID in ac)
                                    {
                                        AttributeReference ar = arID.GetObject(OpenMode.ForWrite) as AttributeReference;
                                        if(ar.HasFields)
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

}
