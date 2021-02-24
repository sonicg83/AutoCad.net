// (C) Copyright 2019 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(coordinatedimension.MyPlugin))]

namespace coordinatedimension
{

    // This class is instantiated by AutoCAD once and kept alive for the 
    // duration of the session. If you don't do any one time initialization 
    // then you should remove this class.
    public class MyPlugin : IExtensionApplication
    {

        void IExtensionApplication.Initialize()
        {
            // Add one time initialization here
            // One common scenario is to setup a callback function here that 
            // unmanaged code can call. 
            // To do this:
            // 1. Export a function from unmanaged code that takes a function
            //    pointer and stores the passed in value in a global variable.
            // 2. Call this exported function in this function passing delegate.
            // 3. When unmanaged code needs the services of this managed module
            //    you simply call acrxLoadApp() and by the time acrxLoadApp 
            //    returns  global function pointer is initialized to point to
            //    the C# delegate.
            // For more info see: 
            // http://msdn2.microsoft.com/en-US/library/5zwkzwf4(VS.80).aspx
            // http://msdn2.microsoft.com/en-us/library/44ey4b32(VS.80).aspx
            // http://msdn2.microsoft.com/en-US/library/7esfatk4.aspx
            // as well as some of the existing AutoCAD managed apps.

            // Initialize your plug-in application here
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\n已加载坐标标注插件1.03版.");
            ed.WriteMessage("\n命令ZB:标注节点坐标.");
            ed.WriteMessage("\n命令ZBH:标注节点坐标（含标高）.");
            ed.WriteMessage("\n命令ZBA:标注节点坐标（含标高及附加文字).");
            ed.WriteMessage("\n说明1:无需设置字体样式,内含RQZB样式,大字体HZTXT,字体txtd,如显示异常请检查字体文件是否缺失.");
            ed.WriteMessage("\n说明2:为确定标高线长度,增加高程数量级参数,高程小于10为0，10-99为1，以此类推.");
            ed.WriteMessage("\n说明3:1.01版修正了石总发现的一个引线偏离的bug.");
            ed.WriteMessage("\n说明4:1.02版修正了郑总发现的旋转问题.");
            ed.WriteMessage("\n说明5:1.03版修正了郑总发现的另一个旋转问题（旋转角度正负）.");
            ed.WriteMessage("\n说明6:使用愉快：）");
        }

        void IExtensionApplication.Terminate()
        {
            // Do plug-in application clean up here
        }

    }

}
