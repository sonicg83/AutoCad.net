using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace SheetSetLib
{
    public static class Funs
    {
        public static string GetChsNum(int Num)
        {
            string chsnums;
            if(Num <= 10)
            {
                chsnums = "一二三四五六七八九十";
                return chsnums[Num - 1].ToString();         
            }
            else if(Num > 10 && Num <= 99)
            {
                chsnums = "一二三四五六七八九";
                if(Num < 20)
                {
                    return string.Format("十{0}", chsnums[Num % 10 - 1].ToString());
                }
                else
                {
                    if(Num%10 == 0)
                    {
                        return string.Format("{0}十", chsnums[Num / 10 - 1].ToString());
                    }
                    else
                    {
                        return string.Format("{0}十{1}", chsnums[Num / 10 - 1].ToString(), chsnums[Num % 10 - 1].ToString());
                    }
                }
            }
            else if(Num > 99 && Num <= 999)
            {
                chsnums = "零一二三四五六七八九";
                return string.Format("{0}{1}{2}", chsnums[Num / 100].ToString(), chsnums[(Num / 10) % 10].ToString(), chsnums[Num % 10].ToString());
            }
            else
            {
                return "OutOfRange";
            }
        }
        public static string GetPrefix(string code,string split,int digit,int serial)
        {
            return string.Format("{0}{1}{2}", code, split, serial.ToString().PadLeft(digit,'0'));
        }
    }


    /// <summary>
    /// 描述图纸模型
    /// </summary>
    public class Sheet
    {

        #region Fields
        #endregion
        #region Properties
        public int GlobalSerial { get; set; }
        public int InnerSerial { get; set; }
        public int Digit { get; set; }
        public string SubsetName { get; set; }
        public string SheetSize { get; set; }
        public string Scale { get; set; }
        public string Remark { get; set; }
        public string Draft { get; set; }
        public string Design { get; set; }
        public string Check { get; set; }
        public string Chief { get; set; }
        #endregion

        #region Methods
        public string GetSheetName()
        {
            if (InnerSerial == 0)
            {
                return SubsetName;
            }
            else
            {
                return string.Format("{0}({1})", Funs.GetChsNum(InnerSerial), SubsetName);
            }
        }
        public string GetSheetNum()
        {
            return Funs.GetPrefix("", "", Digit, GlobalSerial);
        }
        public string GetLayoutName()
        {
            return string.Format("{0} {1}", GetSheetNum(), GetSheetName());
        }
        #endregion

        #region Constructor
        public Sheet(int _globalserial,int _innerserial,int _digit,string _subsetname,string _sheetsize,string _scale,string _remark,string _draft,string _design,string _check,string _chief)
        {
            GlobalSerial = _globalserial;
            InnerSerial = _innerserial;
            Digit = _digit;
            SubsetName = _subsetname;
            SheetSize = _sheetsize;
            Scale = _scale;
            Remark = _remark;
            Draft = _draft;
            Design = _design;
            Check = _check;
            Chief = _chief;
        }
        #endregion
    }

    public class Subset
    {
        #region Fields
        #endregion
        #region Properties
        public int StartNum { get; set; }
        public string Name { get; set; }
        public int SheetCount { get; set; }
        public string SheetSize { get; set; }
        public string Scale { get; set; }
        public string Type { get; set; }
        public string Draft { get; set; }
        public string Design { get; set; }
        public string Check { get; set; }
        public string Chief { get; set; }
        public FileInfo ModelFile { get; set; }
        public FileInfo Xref { get; set; }
        public string Remark { get; set; }
        public int Digit { get; set; }
        public string DivisionCode { get; set; }
        public DirectoryInfo ProjectPath { get; set; }
        public FileInfo File { get; set; }
        public ArrayList SubsetList { get; private set; }
        #endregion
        #region Methods
        #endregion
        #region Constructor
        public Subset(int _startnum,string _name,int _sheetcount ,string _sheetsize,string _scale,string _type,string _draft,string _design,string _check,string _chief,string _modelfile,string _xref,string _remark)
        {
            StartNum = _startnum;
            Name = _name;
            SheetCount = _sheetcount;
            SheetSize = _sheetsize;
            Scale = _scale;
            Type = _type;
            Draft = _draft;
            Design = _design;
            Check = _check;
            Chief = _chief;
            ModelFile = new FileInfo(_modelfile);
            Xref = new FileInfo(_xref);
            Remark = _remark;

            for (int i = 0; i < _sheetcount ;i++)
            {
                Sheet newsheet = new Sheet(_startnum+i,i+1,);

            }
        }
        #endregion
    }


    public class Sheetset
    {
        #region Fields
        #endregion
        #region Properties
        

        #endregion
        #region Methods
        #endregion
        #region Constructor
        #endregion
    }
}
