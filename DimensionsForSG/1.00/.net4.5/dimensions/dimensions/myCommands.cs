// (C) Copyright 2019 by  
//
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(dimensions.MyCommands))]

namespace dimensions
{


    public enum TypeOfCurveElements : int         //曲线要素种类枚举，冷弯、热煨和弹敷
    {
        cold = 1,
        hot = 2,
        elastic = 3
    }

    public class MyCommands
    {
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;

        public string RadToDeg(double rad)                              //弧度转成度分的方法
        {
            double deg = rad / Math.PI * 180;
            int degree = (int)deg;
            int min = (int)Math.Round((deg - degree) * 60, 0);
            string s = Convert.ToString(degree) + "%%D" + Convert.ToString(min) + "'";
            return s;
        }

        public ObjectId CreateMyTablesytle()          //创建表格样式
        {
            //Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //Database db = Application.DocumentManager.MdiActiveDocument.Database;
            const string stylename = "GCLCurveElementsTableStyle";
            ObjectId tsId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DBDictionary sd = (DBDictionary)trans.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);
                    if (sd.Contains(stylename))
                    {
                        tsId = sd.GetAt(stylename);
                    }
                    else
                    {
                        TableStyle ts = new TableStyle();
                        ts.FlowDirection = FlowDirection.LeftToRight;   //这是10版.net的bug？
                        ts.HorizontalCellMargin = 0;
                        ts.VerticalCellMargin = 0;

                        sd.UpgradeOpen();
                        tsId = sd.SetAt(stylename, ts);
                        trans.AddNewlyCreatedDBObject(ts, true);
                    }
                    trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！" + EX.ToString());
                }
            }
            return tsId;
        }

        public ObjectId CreateCeDic()            //创建存储于NameObjectsDictionary的DataTable用于保存程序设置
                                                 //弯管类型存储在（0，0），半径在（0，1），半径倍率在（0，2），字高在（0，3），是否显示节点坐标（0，4）                                   
        {

            const string dicname = "GCLCurveElementsConfig";
            ObjectId DicId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DBDictionary dd = (DBDictionary)trans.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                    if (dd.Contains(dicname))
                    {
                        DicId = dd.GetAt(dicname);
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.TableName = dicname;
                        dt.AppendColumn(CellType.Integer, "Type");
                        dt.AppendColumn(CellType.Double, "Diameter");
                        dt.AppendColumn(CellType.Double, "DiameterMultiple");
                        dt.AppendColumn(CellType.Double, "TextH");
                        DataCellCollection Row = new DataCellCollection();
                        DataCell Type = new DataCell();
                        DataCell Diameter = new DataCell();
                        DataCell DiameterMultiple = new DataCell();
                        DataCell TextH = new DataCell();

                        PromptKeywordOptions keyopts = new PromptKeywordOptions("\n输入弯管的类型[冷弯(C)/热煨(H)/弹性敷设(E)]:", "C H E");
                        PromptResult keyresult = ed.GetKeywords(keyopts);
                        if (keyresult.Status == PromptStatus.OK)
                        {
                            switch (keyresult.StringResult)
                            {
                                case "C":
                                    Type.SetInteger((int)TypeOfCurveElements.cold);
                                    break;
                                case "H":
                                    Type.SetInteger((int)TypeOfCurveElements.hot);
                                    break;
                                case "E":
                                    Type.SetInteger((int)TypeOfCurveElements.elastic);
                                    break;
                            }
                            PromptDoubleOptions doubleopts = new PromptDoubleOptions("\n输入弯管外径(mm):");
                            doubleopts.AllowNegative = false;
                            doubleopts.AllowZero = false;

                            PromptDoubleResult doubleresult = ed.GetDouble(doubleopts);
                            if (doubleresult.Status == PromptStatus.OK)
                            {
                                Diameter.SetDouble(doubleresult.Value);

                                doubleopts.Message = "\n输入管径倍率:";
                                doubleresult = ed.GetDouble(doubleopts);
                                if (doubleresult.Status == PromptStatus.OK)
                                {
                                    DiameterMultiple.SetDouble(doubleresult.Value);

                                    doubleopts.Message = "\n输入字高:";
                                    doubleresult = ed.GetDouble(doubleopts);
                                    if (doubleresult.Status == PromptStatus.OK)
                                    {
                                        TextH.SetDouble(doubleresult.Value);
                                        Row.Add(Type);
                                        Row.Add(Diameter);
                                        Row.Add(DiameterMultiple);
                                        Row.Add(TextH);
                                        dt.AppendRow(Row, true);

                                        dd.UpgradeOpen();
                                        DicId = dd.SetAt(dicname, dt);
                                        trans.AddNewlyCreatedDBObject(dt, true);

                                    }

                                }
                            }
                        }
                    }


                    trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！" + EX.ToString());
                }
            }
            return DicId;
        }

        public ObjectId CreatCeTable(ObjectId tableID, Point3d insPoint, double angle)    //创建表格返回表格的objectID
        {
            Table tb = new Table();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    CurveElements thisCE = new CurveElements(
                        (double)dt.GetCellAt(0, 2).Value,
                        (double)dt.GetCellAt(0, 1).Value,
                        insPoint,
                        angle
                        );
                    double TextH = (double)dt.GetCellAt(0, 3).Value;

                    string type = "unknown";
                    switch ((int)dt.GetCellAt(0, 0).Value)
                    {
                        case 1:
                            type = "Rc";
                            break;
                        case 2:
                            type = "Rh";
                            break;
                        case 3:
                            type = "Re";
                            break;
                    }
                    string[] rows =
                        {
                        "E="+thisCE.ApexDistance.ToString ("f2"),
                        "L="+thisCE.Length.ToString("f2"),
                        "T="+thisCE.TangentLength.ToString ("f2"),
                        "R="+thisCE.Radius.ToString("f2"),
                        "a="+RadToDeg(thisCE.Angle),
                        type +"="+ thisCE.TimeOfDiameter.ToString() + "D"
                    };



                    ObjectId tsId = CreateMyTablesytle();
                    if (tsId == null)
                    {
                        tb.TableStyle = db.Tablestyle;
                    }
                    else
                    {
                        tb.TableStyle = tsId;
                    }

                    tb.NumRows = 6;
                    tb.NumColumns = 1;
                    tb.SetRowHeight(TextH + TextH / 3 + TextH / 21 * 2);      //嗯嗯嗯
                    tb.SetColumnWidth(7.2 * TextH);

                    double[] insertP = { insPoint.X, insPoint.Y, insPoint.Z };
                    tb.Position = new Point3d(insertP);
                    for (int i = 0; i < 6; i++)
                    {
                        tb.SetTextHeight(i, 0, TextH);
                        tb.SetTextString(i, 0, rows[i]);
                        tb.SetAlignment(i, 0, CellAlignment.MiddleCenter);
                    }
                    tb.GenerateLayout();
                    BlockTable bt =
        (BlockTable)trans.GetObject(
          db.BlockTableId,
          OpenMode.ForRead
        );
                    BlockTableRecord btr =
                      (BlockTableRecord)trans.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite
                      );
                    btr.AppendEntity(tb);
                    trans.AddNewlyCreatedDBObject(tb, true);

                    trans.Commit();
                }

                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！" + EX.ToString());
                }
            }
            return tb.ObjectId;

        }



        public void CurrentConfig(ObjectId tableID)                     //获取并显示当前程序设置信息
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    string type = "unknown";
                    switch ((int)dt.GetCellAt(0, 0).Value)
                    {
                        case 1:
                            type = "冷弯";
                            break;
                        case 2:
                            type = "热煨";
                            break;
                        case 3:
                            type = "弹性敷设";
                            break;
                    }
                    ed.WriteMessage(
                        "\n当前设置: 弯管类型={0}  弯管外径={1}mm  弯曲半径={2}倍外径  字高={3}",
                        type,
                        dt.GetCellAt(0, 1).Value.ToString(),
                        dt.GetCellAt(0, 2).Value.ToString(),
                        dt.GetCellAt(0, 3).Value.ToString()
                        );
                    trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！" + EX.ToString());
                }

            }
        }

        public void ChangeConfig(ObjectId tableID)           //修改程序设置信息
        {
            DataCellCollection Row = new DataCellCollection();
            DataCell Type = new DataCell();
            DataCell Diameter = new DataCell();
            DataCell DiameterMultiple = new DataCell();
            DataCell TextH = new DataCell();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForWrite);



                    PromptKeywordOptions keyopts = new PromptKeywordOptions("\n输入弯管的类型[冷弯(C)/热煨(H)/弹性敷设(E)]:", "C H E");
                    keyopts.AllowNone = true;

                    switch ((int)dt.GetCellAt(0, 0).Value)
                    {
                        case 1:
                            keyopts.Keywords.Default = "C";
                            break;
                        case 2:
                            keyopts.Keywords.Default = "H";
                            break;
                        case 3:
                            keyopts.Keywords.Default = "E";
                            break;
                    }

                    PromptResult keyresult = ed.GetKeywords(keyopts);
                    if (keyresult.Status == PromptStatus.OK || keyresult.Status == PromptStatus.None)
                    {
                        switch (keyresult.StringResult)
                        {
                            case "C":
                                Type.SetInteger((int)TypeOfCurveElements.cold);
                                break;
                            case "H":
                                Type.SetInteger((int)TypeOfCurveElements.hot);
                                break;
                            case "E":
                                Type.SetInteger((int)TypeOfCurveElements.elastic);
                                break;
                        }
                        PromptDoubleOptions doubleopts = new PromptDoubleOptions("\n输入弯管外径(mm):");
                        doubleopts.AllowNone = true;
                        doubleopts.AllowNegative = false;
                        doubleopts.AllowZero = false;
                        doubleopts.UseDefaultValue = true;
                        doubleopts.DefaultValue = (double)dt.GetCellAt(0, 1).Value;

                        PromptDoubleResult doubleresult = ed.GetDouble(doubleopts);
                        if (doubleresult.Status == PromptStatus.OK || doubleresult.Status == PromptStatus.None)
                        {
                            Diameter.SetDouble(doubleresult.Value);

                            doubleopts.Message = "\n输入管径倍率:";
                            doubleopts.DefaultValue = (double)dt.GetCellAt(0, 2).Value;
                            doubleresult = ed.GetDouble(doubleopts);
                            if (doubleresult.Status == PromptStatus.OK || doubleresult.Status == PromptStatus.None)
                            {
                                DiameterMultiple.SetDouble(doubleresult.Value);

                                doubleopts.Message = "\n输入字高:";
                                doubleopts.DefaultValue = (double)dt.GetCellAt(0, 3).Value;
                                doubleresult = ed.GetDouble(doubleopts);
                                if (doubleresult.Status == PromptStatus.OK || doubleresult.Status == PromptStatus.None)
                                {
                                    TextH.SetDouble(doubleresult.Value);
                                    Row.Add(Type);
                                    Row.Add(Diameter);
                                    Row.Add(DiameterMultiple);
                                    Row.Add(TextH);
                                    dt.SetRowAt(0, Row, true);
                                }
                            }
                        }
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！" + EX.ToString());
                }
                trans.Commit();
            }

        }
        public ObjectId ChooseAnotherLine(ObjectId FLineID)
        {
            ObjectId SlineID = ObjectId.Null;
            PromptEntityOptions opt = new PromptEntityOptions("\n选择第二条直线");
            opt.SetRejectMessage("\n只能选择直线哦！");
            opt.AddAllowedClass(typeof(Line), true);     //必须先使用SetRejectMessage设置提示才能使用这个方法，否则会报错。
            opt.AllowObjectOnLockedLayer = true;
            opt.AllowNone = false;
            PromptEntityResult result = ed.GetEntity(opt);
            if (result.Status != PromptStatus.OK)
            {
                return ObjectId.Null;
            }

            SlineID = result.ObjectId;
            int count = 0;
            while (FLineID == SlineID)
            {
                count++;
                if (count == 3)
                {
                    ed.WriteMessage("\n你是不是傻？");
                }
                if (count == 4)
                {
                    ed.WriteMessage("\n再见！");
                    return ObjectId.Null;
                }
                ed.WriteMessage("\n不要选择同一条直线哦!");
                opt.Message = "\n重新选择第二条直线";
                result = ed.GetEntity(opt);
                if (result.Status != PromptStatus.OK)
                {
                    return ObjectId.Null;
                }
                SlineID = result.ObjectId;
            }
            return SlineID;

        }

        [CommandMethod("CE", CommandFlags.Modal)]
        public void DimOfCE()
        {


            ObjectId dicId = CreateCeDic();
            ObjectId IDofL1 = ObjectId.Null;
            ObjectId IDofL2 = ObjectId.Null;
            if (dicId != ObjectId.Null)
            {
                CurrentConfig(dicId);
                //开始选择直线
                PromptEntityOptions opt = new PromptEntityOptions("\n选择第一条直线或[更改设置(S)]:", "S");
                opt.SetRejectMessage("\n只能选择直线哦！");
                opt.AddAllowedClass(typeof(Line), true);     //必须先使用SetRejectMessage设置提示才能使用这个方法，否则会报错。
                opt.AllowObjectOnLockedLayer = true;
                opt.AllowNone = false;
                PromptEntityResult result = ed.GetEntity(opt);
                while (result.Status == PromptStatus.Keyword)
                {
                    ChangeConfig(dicId);
                    result = ed.GetEntity(opt);
                }
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                IDofL1 = result.ObjectId;
                IDofL2 = ChooseAnotherLine(IDofL1);
                if (IDofL2 == ObjectId.Null)
                {
                    return;
                }

            }
            //直线选择完毕
            //开始获取交点
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Line L1 = trans.GetObject(IDofL1, OpenMode.ForRead) as Line;
                    Line L2 = trans.GetObject(IDofL2, OpenMode.ForRead) as Line;
                    Point3dCollection intersectionpoints = new Point3dCollection();
                    L1.IntersectWith
                        (
                            L2,
                            Intersect.OnBothOperands,
                            intersectionpoints,
                            0,
                            0
                         );
                    while (intersectionpoints.Count == 0)
                    {
                        ed.WriteMessage("\n两条直线不相交，需要重新选择第二条直线");
                        IDofL2 = ChooseAnotherLine(IDofL1);
                        L1 = trans.GetObject(IDofL1, OpenMode.ForRead) as Line;
                        L2 = trans.GetObject(IDofL2, OpenMode.ForRead) as Line;
                        intersectionpoints = new Point3dCollection();
                        L1.IntersectWith
                            (
                                L2,
                                Intersect.OnBothOperands,
                                intersectionpoints,
                                0,
                                0
                             );
                    }
                    Point3d node = intersectionpoints[0];
                    //获取交点结束
                    //开始获取夹角
                    double AngleOfL1 = L1.Angle;
                    double AngleOfL2 = L2.Angle;
                    double angle = Math.Abs(AngleOfL1 - AngleOfL2);
                    if (angle < Math.PI / 2)
                    {
                        angle = Math.PI - angle;
                    }
                    else if (angle > Math.PI)
                    {
                        angle = angle - Math.PI;
                    }
                    //获取夹角结束
                    Table tb = (Table)trans.GetObject(CreatCeTable(dicId, node, angle), OpenMode.ForWrite);
                    jigCE jigger = new jigCE(tb);
                    Line drgline = new Line(jigger.basePT, jigger.basePT);
                    jigger.Leader = drgline;



                    PromptResult jigresult = ed.Drag(jigger);
                    if (jigresult.Status == PromptStatus.OK)
                    {
                        jigger.TransformEnties();
                        BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        btr.AppendEntity(jigger.Leader);
                        trans.AddNewlyCreatedDBObject(jigger.Leader, true);
                        trans.Commit();
                        //ed.WriteMessage("\n起点坐标,x={0},y={1}", jigger.basePT.X, jigger.basePT.Y);//测试用
                        //ed.WriteMessage("\n终点坐标,x={0},y={1}", jigger.dargPT.X, jigger.dargPT.Y);//测试用
                    }
                    else
                    {
                        trans.Abort();
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！" + EX.ToString());
                }
            }








        }

    }
}


public struct CurveElements //构造结构“曲线要素”
{
    private double _TimesOfDiameter;            //管径倍数
    public double TimeOfDiameter
    {
        get { return _TimesOfDiameter; }
        set { _TimesOfDiameter = value; }
    }
    private double _OuterDiameterOfPipe;
    public double OuterDiameterOfPipe           //管道外径
    {
        get { return _OuterDiameterOfPipe; }
        set { _OuterDiameterOfPipe = value; }
    }
    private Point3d _Node;
    public Point3d Node                           //节点
    {
        get { return _Node; }
    }
    private double _Radian;
    public double Angle          //转角α，输出弧度  
    {
        get { return _Radian - Math.PI / 2; }
    }


    public double TangentLength   //切线长T
    {
        get { return Math.Tan(Math.PI / 2 - _Radian / 2) * _TimesOfDiameter * _OuterDiameterOfPipe / 1000; }
    }

    public double ApexDistance    //外矢距E
    {
        get { return (1 - Math.Sin(_Radian / 2)) / Math.Sin(_Radian / 2) * _TimesOfDiameter * _OuterDiameterOfPipe / 1000; }
    }

    public double Length          //弧长L
    {
        get { return (Math.PI - _Radian) * _TimesOfDiameter * _OuterDiameterOfPipe / 1000; }
    }
    public double Radius         //半径R
    {
        get { return _TimesOfDiameter * _OuterDiameterOfPipe / 1000; }
    }

    public CurveElements(
                           double InputTimesOfDiameter,
                           double InputOuterDiameterOfPipe,
                           Point3d InputNode,
                           double InputRadian

                        )  //构造“曲线要素”结构需要输入节点、管径，管径倍数和转角（弧度）
    {
        this._TimesOfDiameter = InputTimesOfDiameter;
        this._OuterDiameterOfPipe = InputOuterDiameterOfPipe;
        this._Node = InputNode;
        this._Radian = InputRadian;
    }
}

public class jigCE : DrawJig
{
    #region Fields
    private Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    private Database db = Application.DocumentManager.MdiActiveDocument.Database;
    private Point3d mbasePT = new Point3d();
    private Point3d mdragPT = new Point3d();
    private Line mleader = new Line();
    private Table mtableob = new Table();
    #endregion
    #region Properties
    public jigCE(Table insTB)
    {
        this.mbasePT = insTB.Position;
        this.mtableob = insTB;
    }
    public Point3d basePT
    {
        get { return mbasePT; }
    }

    public Point3d dargPT
    {
        get { return mdragPT; }
        set { mdragPT = value; }
    }
    public Line Leader
    {
        get { return mleader; }
        set { mleader = value; }
    }
    public Table Tableob
    {
        get { return mtableob; }
    }
    public Matrix3d UCS
    {
        get { return ed.CurrentUserCoordinateSystem; }
    }
    #endregion

    #region Methods
    public void TransformEnties()
    {
        Vector3d UserXaxis = db.Ucsxdir;
        Vector3d WXaxis = new Vector3d(1, 0, 0);
        Vector3d Zaxis = new Vector3d(0, 0, 1);
        double Rangle = UserXaxis.GetAngleTo(WXaxis);
        Matrix3d Rmat = Matrix3d.Rotation(Rangle, Zaxis, mdragPT);            //旋转矩阵
        Vector3d VT = new Vector3d(1, 1, 1);                                        //插入点转换向量
        if (mdragPT.TransformBy(UCS.Inverse()).X >= mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y >= mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(0, 0, 0);
        }
        if (mdragPT.TransformBy(UCS.Inverse()).X < mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y >= mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(-mtableob.Width, 0, 0);
        }
        if (mdragPT.TransformBy(UCS.Inverse()).X < mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y < mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(-mtableob.Width, -mtableob.Height, 0);
        }
        if (mdragPT.TransformBy(UCS.Inverse()).X >= mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y < mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(0, -mtableob.Height, 0);
        }
        Matrix3d mat = Matrix3d.Displacement(mbasePT.GetVectorTo(mdragPT) + VT); //位移矩阵
        mtableob.TransformBy(mat.PreMultiplyBy(Rmat));      //位移矩阵左乘旋转矩阵获得变形矩阵
    }
    #endregion

    #region Overrides
    protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
    {
        Vector3d UserXaxis = db.Ucsxdir;
        Vector3d WXaxis = new Vector3d(1, 0, 0);
        Vector3d Zaxis = new Vector3d(0, 0, 1);
        double Rangle = UserXaxis.GetAngleTo(WXaxis);
        Matrix3d Rmat = Matrix3d.Rotation(Rangle, Zaxis, mdragPT);
        Vector3d VT = new Vector3d(1, 1, 1);                                        //插入点转换向量
        if (mdragPT.TransformBy(UCS.Inverse()).X >= mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y >= mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(0, 0, 0);
        }
        if (mdragPT.TransformBy(UCS.Inverse()).X < mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y >= mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(-mtableob.Width, 0, 0);
        }
        if (mdragPT.TransformBy(UCS.Inverse()).X < mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y < mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(-mtableob.Width, -mtableob.Height, 0);
        }
        if (mdragPT.TransformBy(UCS.Inverse()).X >= mbasePT.TransformBy(UCS.Inverse()).X && mdragPT.TransformBy(UCS.Inverse()).Y < mbasePT.TransformBy(UCS.Inverse()).Y)
        {
            VT = new Vector3d(0, -mtableob.Height, 0);
        }
        Matrix3d mat = Matrix3d.Displacement(mbasePT.GetVectorTo(mdragPT) + VT);
        WorldGeometry geo = draw.Geometry;
        if (geo != null)
        {
            geo.Draw(mleader);
            geo.PushModelTransform(mat.PreMultiplyBy(Rmat));
            geo.Draw(mtableob);
            geo.PopModelTransform();
        }
        return true;
    }

    protected override SamplerStatus Sampler(JigPrompts prompts)
    {
        JigPromptPointOptions opts = new JigPromptPointOptions("\n选择插入点");
        opts.UseBasePoint = false;
        PromptPointResult ptresult = prompts.AcquirePoint(opts);          //获取的是WCS坐标
        if (ptresult.Status == PromptStatus.Cancel || ptresult.Status == PromptStatus.Error)
        {
            return SamplerStatus.Cancel;
        }
        if (ptresult.Value.IsEqualTo(mdragPT))
        {
            return SamplerStatus.NoChange;
        }
        mdragPT = ptresult.Value;
        mleader.EndPoint = mdragPT;                             //这一句放到worlddraw里会有bug         
        return SamplerStatus.OK;
    }
    #endregion
}



