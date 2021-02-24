// (C) Copyright 2019 by  GCL
//
using System;
using System.Collections;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(coordinatedimension.MyCommands))]

namespace coordinatedimension
{
    public class NodeDim
    {
        #region Fields
        private Point3d _basePT;                   //坐标点
        private Point3d _insPT;                    // 插入点

        private string _Xcoordinate;
        private string _Ycoordinate;
        private string _ExtraText = "附加说明";
        private string _Gheight = "地面标高";
        private string _Pheight = "管底标高";
        private double _FactorOfGapLine = 0.6;                 //坐标组与标高组的间隔与字高的系数
        private double _TextHeight;              //字高

        private double _FactorOfTextMagin = 0.4;      //文字与坐标线和标高线的垂直间距与字高的系数（与ZB相同）

        private double _FactorOfGapText = 0.4;     //文字与线边缘的距离与字高的系数   
        private int _EH;                       //标高数量级     


        #endregion

        #region Properties
        public Point3d BasePT
        {
            get { return _basePT; }
        }
        public Point3d InsPT
        {
            get { return _insPT; }
        }
        public string Xstring
        {
            get { return _Xcoordinate; }
        }
        public string Ystring
        {
            get { return _Ycoordinate; }
        }
        public string Extrastring
        {
            get { return _ExtraText; }
            set { _ExtraText = value; }
        }
        public string GHeight
        {
            get { return _Gheight; }
            set { _Gheight = value; }
        }
        public string PHeight
        {
            get { return _Pheight; }
            set { _Pheight = value; }
        }

        public double TextHeight
        {
            get { return _TextHeight; }
            set { _TextHeight = value; }
        }
        public double FactorOfTextMargin
        {
            get { return _FactorOfTextMagin; }
            set { _FactorOfTextMagin = value; }
        }
        public double FactorOfGapText
        {
            get { return _FactorOfGapText; }
            set { _FactorOfGapText = value; }
        }
        public double FactorOfGapLine
        {
            get { return _FactorOfGapLine; }
            set { _FactorOfGapLine = value; }
        }
        public int EH
        {
            get { return _EH; }
            set { _EH = value; }
        }
        public Point3d ClineStartPT
        {
            get
            {
                return _insPT;
            }
        }
        public Point3d ClineEndPT
        {
            get
            {
                return new Point3d(_insPT.X + getClineLength(), _insPT.Y, _insPT.Z);
            }
        }
        public Point3d HlineStartPT
        {
            get
            {
                return new Point3d(_insPT.X + getClineLength() + _TextHeight * _FactorOfGapLine, InsPT.Y, _insPT.Z);
            }
        }
        public Point3d HlineStartPTL
        {
            get
            {
                return new Point3d(_insPT.X - _TextHeight * _FactorOfGapLine, InsPT.Y, _insPT.Z);
            }
        }
        public Point3d HlineEndPT
        {
            get { return new Point3d(_insPT.X + getClineLength() + _TextHeight * _FactorOfGapLine + getHlineLength(), InsPT.Y, _insPT.Z); }
        }
        public Point3d HlineEndPTL
        {
            get { return new Point3d(_insPT.X - _TextHeight * _FactorOfGapLine - getHlineLength(), InsPT.Y, _insPT.Z); }
        }
        public Point3d XTextPT
        {
            get
            {
                return new Point3d(
                _insPT.X + _FactorOfGapText * _TextHeight,
                _insPT.Y + _FactorOfTextMagin * _TextHeight,
                _insPT.Z
                );
            }
        }
        public Point3d YTextPT
        {
            get
            {
                return new Point3d(
                    _insPT.X + _FactorOfGapText * _TextHeight,
                    _insPT.Y - _FactorOfTextMagin * _TextHeight - _TextHeight,
                    _insPT.Z
                    );
            }
        }
        public Point3d ExtraTextPT
        {
            get
            {
                return new Point3d(
                _insPT.X + _FactorOfGapText * _TextHeight,
                _insPT.Y + FactorOfTextMargin * _TextHeight + _TextHeight + FactorOfTextMargin * _TextHeight,
                _insPT.Z
                );
            }
        }
        public Point3d GHTextPT
        {
            get
            {
                return new Point3d(
                    _insPT.X + getClineLength() + _TextHeight * _FactorOfGapLine + _FactorOfGapText * _TextHeight,
                    _insPT.Y + FactorOfTextMargin * _TextHeight,
                    _insPT.Z
                    );
            }
        }
        public Point3d GHTextPTL
        {
            get
            {
                return new Point3d(
                    _insPT.X - _TextHeight * _FactorOfGapLine - getHlineLength() + _FactorOfGapText * _TextHeight,
                    _insPT.Y + FactorOfTextMargin * _TextHeight,
                    _insPT.Z
                    );
            }
        }
        public Point3d PHTextPT
        {
            get
            {
                return new Point3d(
                    _insPT.X + getClineLength() + _TextHeight * _FactorOfGapLine + _FactorOfGapText * _TextHeight,
                    _insPT.Y - FactorOfTextMargin * _TextHeight - _TextHeight,
                    _insPT.Z
                    );
            }
        }
        public Point3d PHTextPTL
        {
            get
            {
                return new Point3d(
                    _insPT.X - _TextHeight * _FactorOfGapLine - getHlineLength() + _FactorOfGapText * _TextHeight,
                    _insPT.Y - FactorOfTextMargin * _TextHeight - _TextHeight,
                    _insPT.Z
                    );
            }
        }


        #endregion

        #region Constructor
        public NodeDim(Point3d Bpoint, Point3d Ipoint, double height, int eoh)
        {
            this._basePT = Bpoint;
            this._insPT = Ipoint;
            this._Xcoordinate = "X=" + Bpoint.Y.ToString("f3");
            this._Ycoordinate = "Y=" + Bpoint.X.ToString("f3");
            this._TextHeight = height;
            this._EH = eoh;




        }
        #endregion

        #region Methods
        public double getClineLength()             //坐标线长度 字高X0.5X字符数（字符宽度比例0.7）
        {
            double l;
            if (_Xcoordinate.Length > _Ycoordinate.Length)
            {
                l = _TextHeight * 0.5 * _Xcoordinate.Length + 2 * _TextHeight * _FactorOfGapText;
            }

            else
            {
                l = _TextHeight * 0.5 * _Ycoordinate.Length + 2 * _TextHeight * _FactorOfGapText;
            }
            return l;
        }
        public double getHlineLength()              //标高线长度  字高X0.5X（字符数-0.5） （字符宽度比例0.7）
        {
            return _TextHeight * 0.5 * (_EH + 1 + 3 - 0.5) + 2 * _TextHeight * _FactorOfGapText;
        }




        #endregion

    }
    public class Dimjig : DrawJig
    {
        #region Fields
        private Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        private Database db = Application.DocumentManager.MdiActiveDocument.Database;
        private Point3d mbasePT = new Point3d();
        private Point3d mdragPT = new Point3d();
        private Line mleader = new Line();
        private DBObjectCollection mEntities = new DBObjectCollection();
        private DBObjectCollection mEntitiesL = new DBObjectCollection();
        private bool mIsRight = true;


        #endregion
        #region Constructors
        public Dimjig(Point3d basePT, DBObjectCollection entities, DBObjectCollection entitiesL)
        {
            this.mbasePT = basePT.TransformBy(UCS);
            this.mEntities = entities;
            this.mEntitiesL = entitiesL;

        }
        #endregion
        #region Properties
        public Point3d basePT
        {
            get { return mbasePT; }
            set { mbasePT = value; }
        }
        public Point3d dragPT
        {
            get { return mdragPT; }
            set { mdragPT = value; }
        }
        public Line leader
        {
            get { return mleader; }
            set { mleader = value; }
        }
        public Matrix3d UCS
        {
            get { return ed.CurrentUserCoordinateSystem; }
        }

        public DBObjectCollection EntityList
        {
            get { return mEntities; }
        }
        public DBObjectCollection EntityListL
        {
            get { return mEntitiesL; }
        }
        public bool IsRight
        {
            get { return mIsRight; }
        }
        #endregion
        #region Methods

        public void TransformEnties()
        {
            Vector3d UserXaxis = db.Ucsxdir;
            Vector3d WXaxis = new Vector3d(1, 0, 0);
            Vector3d Zaxis = new Vector3d(0, 0, 1);
            double Rangle = UserXaxis.GetAngleTo(WXaxis);
            if(UserXaxis.Y<0)
            {
                Zaxis=Zaxis.Negate();                                            //1.03版修改，修正角度旋转正负问题
            }
            Matrix3d Rmat = Matrix3d.Rotation(Rangle, Zaxis, mdragPT);            //旋转矩阵                                                
            if (mdragPT.TransformBy(UCS.Inverse()).X >= mbasePT.TransformBy(UCS.Inverse()).X)
            {
                mIsRight = true;
                Matrix3d mat = Matrix3d.Displacement(mbasePT.GetVectorTo(mdragPT)).PreMultiplyBy(Rmat);  //旋转矩阵左乘位移矩阵
                foreach (Entity ent in mEntities)
                {
                    ent.TransformBy(mat);
                }
            }
            else
            {
                mIsRight = false;
                Line L = (Line)mEntitiesL[0];
                Vector3d Vt = new Vector3d(L.Length, 0, 0);
                Matrix3d mat = Matrix3d.Displacement(mbasePT.GetVectorTo(mdragPT) - Vt).PreMultiplyBy(Rmat);//旋转矩阵左乘位移矩阵，然后镜像矩阵左乘前结果
                foreach (Entity ent in mEntitiesL)
                {
                    ent.TransformBy(mat);
                }
            }
        }
        #endregion
        #region Overrides
        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
        {
            mleader.EndPoint = mdragPT;
            Vector3d UserXaxis = db.Ucsxdir;
            Vector3d WXaxis = new Vector3d(1, 0, 0);
            Vector3d Zaxis = new Vector3d(0, 0, 1);
            double Rangle = UserXaxis.GetAngleTo(WXaxis);
            if (UserXaxis.Y < 0)
            {
                Zaxis = Zaxis.Negate();                                            //1.03版修改，修正角度旋转正负问题
            }
            Matrix3d Rmat = Matrix3d.Rotation(Rangle, Zaxis, mdragPT);            //旋转矩阵                                                
            if (mdragPT.TransformBy(UCS.Inverse()).X >= mbasePT.TransformBy(UCS.Inverse()).X)
            {
                Matrix3d mat = Matrix3d.Displacement(mbasePT.GetVectorTo(mdragPT)).PreMultiplyBy(Rmat);  //旋转矩阵左乘位移矩阵
                WorldGeometry geo = draw.Geometry;
                if (geo != null)
                {
                    geo.Draw(mleader);
                    geo.PushModelTransform(mat);
                    foreach (Entity ent in mEntities)
                    {
                        geo.Draw(ent);
                    }
                    geo.PopModelTransform();
                }
            }
            else
            {
                Line L = (Line)mEntitiesL[0];
                Vector3d Vt = new Vector3d(L.Length, 0, 0);
                Matrix3d mat = Matrix3d.Displacement(mbasePT.GetVectorTo(mdragPT) - Vt).PreMultiplyBy(Rmat);//旋转矩阵左乘位移矩阵
                WorldGeometry geo = draw.Geometry;
                if (geo != null)
                {
                    geo.Draw(mleader);
                    geo.PushModelTransform(mat);
                    foreach (Entity ent in mEntitiesL)
                    {
                        geo.Draw(ent);
                    }
                    geo.PopModelTransform();
                }
            }
            return true;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions opts = new JigPromptPointOptions("\n选择引出点");
            opts.UseBasePoint = false;
            PromptPointResult ptresult = prompts.AcquirePoint(opts);
            if (ptresult.Status == PromptStatus.Cancel || ptresult.Status == PromptStatus.Error)
            {
                return SamplerStatus.Cancel;
            }
            if (ptresult.Value.IsEqualTo(mdragPT))
            {
                return SamplerStatus.NoChange;
            }
            mdragPT = ptresult.Value;
            return SamplerStatus.OK;
        }
        #endregion
    }


    public class MyCommands
    {
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        Database db = Application.DocumentManager.MdiActiveDocument.Database;

        #region 创建文字样式的方法
        public ObjectId CreatMyTextStyle()
        {
            ObjectId StyleID = ObjectId.Null;
            const string StyleName = "RQBZ";
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    TextStyleTable Tst = (TextStyleTable)trans.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                    if (Tst.Has(StyleName))
                    {
                        StyleID = Tst[StyleName];     //来自这个链接 https://www.keanw.com/2012/08/a-handy-jig-for-creating-autocad-text-using-net-part-2.html
                    }
                    else
                    {
                        TextStyleTableRecord MyTextStyle = new TextStyleTableRecord();
                        MyTextStyle.FileName = "txtd.shx";
                        MyTextStyle.BigFontFileName = "hztxt.shx";
                        MyTextStyle.Name = "RQBZ";
                        MyTextStyle.XScale = 0.7;
                        Tst.UpgradeOpen();
                        StyleID = Tst.Add(MyTextStyle);
                        trans.AddNewlyCreatedDBObject(MyTextStyle, true);
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return StyleID;
        }

        #endregion
        #region 创建标注设置的方法
        public ObjectId CreateZBDic()
        {
            ObjectId TableID = ObjectId.Null;
            const string DicName = "GCLZBConfig";
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DBDictionary dd = (DBDictionary)trans.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                    if (dd.Contains(DicName))
                    {
                        TableID = dd.GetAt(DicName);
                    }
                    else
                    {
                        DataTable dt = new DataTable();
                        dt.TableName = DicName;
                        dt.AppendColumn(CellType.Double, "TextHeight");
                        dt.AppendColumn(CellType.Integer, "HeightOderOfMagnitude");
                        DataCellCollection Row = new DataCellCollection();
                        DataCell TH = new DataCell();
                        DataCell HM = new DataCell();

                        PromptDoubleOptions doubleopts = new PromptDoubleOptions("\n输入字高:");
                        doubleopts.AllowNegative = false;
                        doubleopts.AllowZero = false;
                        PromptDoubleResult doubleresult = ed.GetDouble(doubleopts);
                        if (doubleresult.Status == PromptStatus.OK)
                        {
                            TH.SetDouble(doubleresult.Value);

                            PromptIntegerOptions intopts = new PromptIntegerOptions("\n输入高程数量级(高程在10以内为0，100以内为1，以此类推）:");
                            intopts.AllowNegative = false;
                            PromptIntegerResult intresult = ed.GetInteger(intopts);
                            if (intresult.Status == PromptStatus.OK)
                            {
                                HM.SetInteger(intresult.Value);

                                Row.Add(TH);
                                Row.Add(HM);
                                dt.AppendRow(Row, true);
                                dd.UpgradeOpen();
                                TableID = dd.SetAt(DicName, dt);
                                trans.AddNewlyCreatedDBObject(dt, true);
                            }
                        }
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return TableID;
        }
        #endregion
        #region 显示当前标注设置的方法
        public void CurrentZBConfig(ObjectId tableID)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    ed.WriteMessage(
                        "\n当前设置: 字高={0}  高程数量级={1}",
                        dt.GetCellAt(0, 0).Value.ToString(),
                        dt.GetCellAt(0, 1).Value.ToString()
                        );

                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }

        }

        #endregion
        #region 修改当前标注设置的方法
        public void ChangeZBConfig(ObjectId tableID)
        {
            DataCellCollection Row = new DataCellCollection();
            DataCell TH = new DataCell();
            DataCell HM = new DataCell();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForWrite);
                    PromptDoubleOptions doubleopts = new PromptDoubleOptions("\n输入字高:");
                    doubleopts.AllowNone = true;
                    doubleopts.AllowNegative = false;
                    doubleopts.AllowZero = false;
                    doubleopts.UseDefaultValue = true;
                    doubleopts.DefaultValue = (double)dt.GetCellAt(0, 0).Value;
                    PromptDoubleResult doubleresult = ed.GetDouble(doubleopts);
                    if (doubleresult.Status == PromptStatus.OK || doubleresult.Status == PromptStatus.None)
                    {
                        TH.SetDouble(doubleresult.Value);

                        PromptIntegerOptions intopts = new PromptIntegerOptions("\n输入高程数量级(高程在10以内为0，100以内为1，以此类推）:");
                        intopts.AllowNone = true;
                        intopts.AllowNegative = false;
                        intopts.UseDefaultValue = true;
                        intopts.DefaultValue = (int)dt.GetCellAt(0, 1).Value;
                        PromptIntegerResult intresult = ed.GetInteger(intopts);
                        if (intresult.Status == PromptStatus.OK || intresult.Status == PromptStatus.None)
                        {
                            HM.SetInteger(intresult.Value);

                            Row.Add(TH);
                            Row.Add(HM);
                            dt.SetRowAt(0, Row, true);
                        }
                    }

                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }

        }
        #endregion
        #region 创建标注实体(所有组件）
        public DBObjectCollection CreateDim(Point3d basePT, Point3d insPT, ObjectId tableID)
        {

            DBObjectCollection DimEntity = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    NodeDim Dim = new NodeDim(basePT, insPT, (double)dt.GetCellAt(0, 0).Value, (int)dt.GetCellAt(0, 1).Value);
                    Line Cline = new Line(Dim.ClineStartPT, Dim.ClineEndPT);
                    Line Hline = new Line(Dim.HlineStartPT, Dim.HlineEndPT);
                    DBText XText = new DBText();
                    XText.TextString = Dim.Xstring;
                    XText.Position = Dim.XTextPT;
                    XText.Height = Dim.TextHeight;
                    XText.WidthFactor = 0.7;

                    DBText YText = new DBText();
                    YText.TextString = Dim.Ystring;

                    YText.Position = Dim.YTextPT;
                    YText.Height = Dim.TextHeight;
                    YText.WidthFactor = 0.7;

                    DBText GHText = new DBText();
                    GHText.TextString = Dim.GHeight;
                    GHText.Position = Dim.GHTextPT;
                    GHText.Height = Dim.TextHeight;
                    GHText.WidthFactor = 0.7;

                    DBText PHText = new DBText();
                    PHText.TextString = Dim.PHeight;

                    PHText.Position = Dim.PHTextPT;
                    PHText.Height = Dim.TextHeight;
                    PHText.WidthFactor = 0.7;

                    DBText EXText = new DBText();
                    EXText.TextString = Dim.Extrastring;
                    EXText.Position = Dim.ExtraTextPT;
                    EXText.Height = Dim.TextHeight;
                    EXText.WidthFactor = 0.7;



                    DimEntity.Add(Cline);
                    DimEntity.Add(Hline);
                    DimEntity.Add(XText);
                    DimEntity.Add(YText);
                    DimEntity.Add(GHText);
                    DimEntity.Add(PHText);
                    DimEntity.Add(EXText);


                }

                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return DimEntity;
        }
        #endregion
        #region 创建标注实体-左侧（所有组件）
        public DBObjectCollection CreateDimL(Point3d basePT, Point3d insPT, ObjectId tableID)
        {

            DBObjectCollection DimEntity = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    NodeDim Dim = new NodeDim(basePT, insPT, (double)dt.GetCellAt(0, 0).Value, (int)dt.GetCellAt(0, 1).Value);
                    Line Cline = new Line(Dim.ClineStartPT, Dim.ClineEndPT);
                    Line Hline = new Line(Dim.HlineStartPTL, Dim.HlineEndPTL);
                    DBText XText = new DBText();
                    XText.TextString = Dim.Xstring;
                    XText.Position = Dim.XTextPT;
                    XText.Height = Dim.TextHeight;
                    XText.WidthFactor = 0.7;

                    DBText YText = new DBText();
                    YText.TextString = Dim.Ystring;

                    YText.Position = Dim.YTextPT;
                    YText.Height = Dim.TextHeight;
                    YText.WidthFactor = 0.7;

                    DBText GHText = new DBText();
                    GHText.TextString = Dim.GHeight;
                    GHText.Position = Dim.GHTextPTL;
                    GHText.Height = Dim.TextHeight;
                    GHText.WidthFactor = 0.7;

                    DBText PHText = new DBText();
                    PHText.TextString = Dim.PHeight;

                    PHText.Position = Dim.PHTextPTL;
                    PHText.Height = Dim.TextHeight;
                    PHText.WidthFactor = 0.7;

                    DBText EXText = new DBText();
                    EXText.TextString = Dim.Extrastring;
                    EXText.Position = Dim.ExtraTextPT;
                    EXText.Height = Dim.TextHeight;
                    EXText.WidthFactor = 0.7;



                    DimEntity.Add(Cline);
                    DimEntity.Add(Hline);
                    DimEntity.Add(XText);
                    DimEntity.Add(YText);
                    DimEntity.Add(GHText);
                    DimEntity.Add(PHText);
                    DimEntity.Add(EXText);


                }

                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return DimEntity;
        }
        #endregion
        #region 创建标注实体(仅坐标）
        public DBObjectCollection CreateDimOnlyXY(Point3d basePT, Point3d insPT, ObjectId tableID)
        {

            DBObjectCollection DimEntity = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    NodeDim Dim = new NodeDim(basePT, insPT, (double)dt.GetCellAt(0, 0).Value, (int)dt.GetCellAt(0, 1).Value);
                    Line Cline = new Line(Dim.ClineStartPT, Dim.ClineEndPT);

                    DBText XText = new DBText();
                    XText.TextString = Dim.Xstring;
                    XText.Position = Dim.XTextPT;
                    XText.Height = Dim.TextHeight;
                    XText.WidthFactor = 0.7;

                    DBText YText = new DBText();
                    YText.TextString = Dim.Ystring;
                    YText.Position = Dim.YTextPT;
                    YText.Height = Dim.TextHeight;
                    YText.WidthFactor = 0.7;

                    DimEntity.Add(Cline);
                    DimEntity.Add(XText);
                    DimEntity.Add(YText);
                }

                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return DimEntity;
        }
        #endregion
        #region 创建标注实体(含标高）
        public DBObjectCollection CreateDimWithHeight(Point3d basePT, Point3d insPT, ObjectId tableID)
        {

            DBObjectCollection DimEntity = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    NodeDim Dim = new NodeDim(basePT, insPT, (double)dt.GetCellAt(0, 0).Value, (int)dt.GetCellAt(0, 1).Value);
                    Line Cline = new Line(Dim.ClineStartPT, Dim.ClineEndPT);
                    Line Hline = new Line(Dim.HlineStartPT, Dim.HlineEndPT);
                    DBText XText = new DBText();
                    XText.TextString = Dim.Xstring;
                    XText.Position = Dim.XTextPT;
                    XText.Height = Dim.TextHeight;
                    XText.WidthFactor = 0.7;

                    DBText YText = new DBText();
                    YText.TextString = Dim.Ystring;
                    YText.Position = Dim.YTextPT;
                    YText.Height = Dim.TextHeight;
                    YText.WidthFactor = 0.7;

                    DBText GHText = new DBText();
                    GHText.TextString = Dim.GHeight;
                    GHText.Position = Dim.GHTextPT;
                    GHText.Height = Dim.TextHeight;
                    GHText.WidthFactor = 0.7;

                    DBText PHText = new DBText();
                    PHText.TextString = Dim.PHeight;
                    PHText.Position = Dim.PHTextPT;
                    PHText.Height = Dim.TextHeight;
                    PHText.WidthFactor = 0.7;

                    DimEntity.Add(Cline);
                    DimEntity.Add(Hline);
                    DimEntity.Add(XText);
                    DimEntity.Add(YText);
                    DimEntity.Add(GHText);
                    DimEntity.Add(PHText);



                }

                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return DimEntity;
        }
        #endregion
        #region 创建标注实体-左侧（含标高）
        public DBObjectCollection CreateDimWithHeightL(Point3d basePT, Point3d insPT, ObjectId tableID)
        {

            DBObjectCollection DimEntity = new DBObjectCollection();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DataTable dt = (DataTable)trans.GetObject(tableID, OpenMode.ForRead);
                    NodeDim Dim = new NodeDim(basePT, insPT, (double)dt.GetCellAt(0, 0).Value, (int)dt.GetCellAt(0, 1).Value);
                    Line Cline = new Line(Dim.ClineStartPT, Dim.ClineEndPT);
                    Line Hline = new Line(Dim.HlineStartPTL, Dim.HlineEndPTL);
                    DBText XText = new DBText();
                    XText.TextString = Dim.Xstring;
                    XText.Position = Dim.XTextPT;
                    XText.Height = Dim.TextHeight;
                    XText.WidthFactor = 0.7;

                    DBText YText = new DBText();
                    YText.TextString = Dim.Ystring;
                    YText.Position = Dim.YTextPT;
                    YText.Height = Dim.TextHeight;
                    YText.WidthFactor = 0.7;

                    DBText GHText = new DBText();
                    GHText.TextString = Dim.GHeight;
                    GHText.Position = Dim.GHTextPTL;
                    GHText.Height = Dim.TextHeight;
                    GHText.WidthFactor = 0.7;

                    DBText PHText = new DBText();
                    PHText.TextString = Dim.PHeight;
                    PHText.Position = Dim.PHTextPTL;
                    PHText.Height = Dim.TextHeight;
                    PHText.WidthFactor = 0.7;





                    DimEntity.Add(Cline);
                    DimEntity.Add(Hline);
                    DimEntity.Add(XText);
                    DimEntity.Add(YText);
                    DimEntity.Add(GHText);
                    DimEntity.Add(PHText);



                }

                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }
                trans.Commit();
            }
            return DimEntity;
        }
        #endregion


        #region 标注命令ZBA，含所有组件
        [CommandMethod("ZBA", CommandFlags.Modal)]
        public void MyCommand1() // This method can have any name
        {
            Matrix3d UCS = ed.CurrentUserCoordinateSystem;

            ObjectId StyleID = CreatMyTextStyle();
            db.Textstyle = StyleID;
            ObjectId TableID = CreateZBDic();
            if (TableID != ObjectId.Null)
            {
                CurrentZBConfig(TableID);
                PromptPointOptions opt = new PromptPointOptions("\n插入点或[更改设置(S)]:", "S");
                opt.AllowNone = false;
                PromptPointResult result = ed.GetPoint(opt);
                while (result.Status == PromptStatus.Keyword)
                {
                    ChangeZBConfig(TableID);
                    result = ed.GetPoint(opt);
                }
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                DBObjectCollection EntityList = CreateDim(result.Value.TransformBy(UCS), result.Value.TransformBy(UCS), TableID);
                DBObjectCollection EntityListL = CreateDimL(result.Value.TransformBy(UCS), result.Value.TransformBy(UCS), TableID);
                Dimjig jigger = new Dimjig(result.Value, EntityList, EntityListL);
                Line dragline = new Line(jigger.basePT, jigger.basePT);
                jigger.leader = dragline;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptResult jigresult = ed.Drag(jigger);
                    if (jigresult.Status == PromptStatus.OK)
                    {
                        jigger.TransformEnties();
                        Line Leader = new Line(jigger.basePT, jigger.dragPT);
                        try
                        {
                            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            if (jigger.IsRight)
                            {
                                foreach (Entity ent in jigger.EntityList)
                                {
                                    btr.AppendEntity(ent);
                                    trans.AddNewlyCreatedDBObject(ent, true);
                                }
                            }
                            else
                            {
                                foreach (Entity ent in jigger.EntityListL)
                                {
                                    btr.AppendEntity(ent);
                                    trans.AddNewlyCreatedDBObject(ent, true);
                                }
                            }
                            btr.AppendEntity(Leader);
                            trans.AddNewlyCreatedDBObject(Leader, true);
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception EX)
                        {
                            ed.WriteMessage("\n出错了！" + EX.ToString());
                        }
                        trans.Commit();
                    }
                    else
                    {
                        trans.Abort();
                    }
                }

            }

        }
        #endregion
        #region 标注命令ZB，仅含坐标
        [CommandMethod("ZB", CommandFlags.Modal)]
        public void MyCommand2() // This method can have any name
        {
            Matrix3d UCS = ed.CurrentUserCoordinateSystem;

            ObjectId StyleID = CreatMyTextStyle();
            db.Textstyle = StyleID;
            ObjectId TableID = CreateZBDic();
            if (TableID != ObjectId.Null)
            {
                CurrentZBConfig(TableID);
                PromptPointOptions opt = new PromptPointOptions("\n插入点或[更改设置(S)]:", "S");
                opt.AllowNone = false;
                PromptPointResult result = ed.GetPoint(opt);
                while (result.Status == PromptStatus.Keyword)
                {
                    ChangeZBConfig(TableID);
                    result = ed.GetPoint(opt);
                }
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                DBObjectCollection EntityList = CreateDimOnlyXY(result.Value.TransformBy(UCS), result.Value.TransformBy(UCS), TableID);
                Dimjig jigger = new Dimjig(result.Value, EntityList, EntityList);
                Line dragline = new Line(jigger.basePT, jigger.basePT);
                jigger.leader = dragline;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptResult jigresult = ed.Drag(jigger);
                    if (jigresult.Status == PromptStatus.OK)
                    {
                        jigger.TransformEnties();
                        Line Leader = new Line(jigger.basePT, jigger.dragPT);
                        try
                        {
                            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            if (jigger.IsRight)
                            {
                                foreach (Entity ent in jigger.EntityList)
                                {
                                    btr.AppendEntity(ent);
                                    trans.AddNewlyCreatedDBObject(ent, true);
                                }

                            }
                            else
                            {
                                foreach (Entity ent in jigger.EntityListL)
                                {
                                    btr.AppendEntity(ent);
                                    trans.AddNewlyCreatedDBObject(ent, true);
                                }

                            }
                            btr.AppendEntity(Leader);
                            trans.AddNewlyCreatedDBObject(Leader, true);
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception EX)
                        {
                            ed.WriteMessage("\n出错了！" + EX.ToString());
                        }
                        trans.Commit();
                    }
                    else
                    {
                        trans.Abort();
                    }
                }

            }

        }
        #endregion
        #region 标注命令ZBH，含标高
        [CommandMethod("ZBH", CommandFlags.Modal)]
        public void MyCommand3() // This method can have any name
        {
            Matrix3d UCS = ed.CurrentUserCoordinateSystem;

            ObjectId StyleID = CreatMyTextStyle();
            db.Textstyle = StyleID;
            ObjectId TableID = CreateZBDic();
            if (TableID != ObjectId.Null)
            {
                CurrentZBConfig(TableID);
                PromptPointOptions opt = new PromptPointOptions("\n插入点或[更改设置(S)]:", "S");
                opt.AllowNone = false;
                PromptPointResult result = ed.GetPoint(opt);
                while (result.Status == PromptStatus.Keyword)
                {
                    ChangeZBConfig(TableID);
                    result = ed.GetPoint(opt);
                }
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                DBObjectCollection EntityList = CreateDimWithHeight(result.Value.TransformBy(UCS), result.Value.TransformBy(UCS), TableID);
                DBObjectCollection EntityListL = CreateDimWithHeightL(result.Value.TransformBy(UCS), result.Value.TransformBy(UCS), TableID);
                Dimjig jigger = new Dimjig(result.Value, EntityList, EntityListL);
                Line dragline = new Line(jigger.basePT, jigger.basePT);
                jigger.leader = dragline;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptResult jigresult = ed.Drag(jigger);
                    if (jigresult.Status == PromptStatus.OK)
                    {
                        jigger.TransformEnties();
                        Line Leader = new Line(jigger.basePT, jigger.dragPT);
                        try
                        {
                            BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                            if (jigger.IsRight)
                            {
                                foreach (Entity ent in jigger.EntityList)
                                {
                                    btr.AppendEntity(ent);
                                    trans.AddNewlyCreatedDBObject(ent, true);
                                }

                            }
                            else
                            {
                                foreach (Entity ent in jigger.EntityListL)
                                {
                                    btr.AppendEntity(ent);
                                    trans.AddNewlyCreatedDBObject(ent, true);
                                }

                            }
                            btr.AppendEntity(Leader);
                            trans.AddNewlyCreatedDBObject(Leader, true);

                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception EX)
                        {
                            ed.WriteMessage("\n出错了！" + EX.ToString());
                        }
                        trans.Commit();
                    }
                    else
                    {
                        trans.Abort();
                    }
                }

            }

        }
        #endregion
    }
}
