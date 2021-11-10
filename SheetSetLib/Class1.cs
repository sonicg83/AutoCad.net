using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SheetSetLib
{
    /// <summary>
    /// 描述图纸模型
    /// </summary>
    public class Sheet
    {
        #region Fields
        private int _GlobalSerial;
        private int _InnerSerial;
        private readonly string _DivisionCode;
        private string _SheetName;
        private string _SheetSize;
        private string _Scale;
        private string _Remark;
        private string _Draft;
        private string _Design;
        private string _Check;
        private string _Chief;
        


        #endregion
        #region Properties
        public int GlobalSerial
        {
            get => _GlobalSerial;
            set => _GlobalSerial = value;
        }
        #endregion

        #region Methods
        #endregion

        #region Constructor
        #endregion
    }
}
