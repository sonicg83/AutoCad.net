﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices;

namespace UtilityClass
{
    public static class Utilities
    {
        #region SortToGroup
        public static ArrayList SortToGroup(ArrayList Files,int NumberOfFiles)
        {
            ArrayList GroupList = new ArrayList();
            decimal FileCount = Files.Count;
            decimal groups = FileCount / NumberOfFiles;
            int GroupCount = (int)Math.Ceiling(groups);    //根据文件数量计算分组数（向上取整）
            for (int i = 0; i < GroupCount; i++) 
            {
                ArrayList TempList = new ArrayList();
                if (i == (GroupCount - 1))                                                       //最后一组的情况
                {
                    for (int j = 0; j < (FileCount - NumberOfFiles * i); j++)
                    {
                        TempList.Add(Files[(NumberOfFiles * i + j)]);
                    }
                    GroupList.Add(TempList);
                }
                else                                                                             //其他组的情况
                {
                    for (int j = 0; j < NumberOfFiles; j++)
                    {
                        TempList.Add(Files[(NumberOfFiles * i + j)]);
                    }
                    GroupList.Add(TempList);
                }
            }
            return GroupList;
        }
        #endregion
    }

    #region class IniReader
    public class INIReader
    {
        public string inipath;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(
       string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(
       string section, string key,
       string def, StringBuilder retVal,
       int size, string filePath);
        /// ﹤summary﹥  
        /// 构造方法  
        /// ﹤/summary﹥  
        /// ﹤param name="INIPath"﹥文件路径﹤/param﹥  
        public INIReader(string INIPath)
        {
            inipath = INIPath;
        }
        /// ﹤summary﹥  
        /// 写入INI文件  
        /// ﹤/summary﹥  
        /// ﹤param name="Section"﹥项目名称(如 [TypeName] )﹤/param﹥  
        /// ﹤param name="Key"﹥键﹤/param﹥  
        /// ﹤param name="Value"﹥值﹤/param﹥  
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }
        /// ﹤summary﹥  
        /// 读出INI文件  
        /// ﹤/summary﹥  
        /// ﹤param name="Section"﹥项目名称(如 [TypeName] )﹤/param﹥  
        /// ﹤param name="Key"﹥键﹤/param﹥  
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, this.inipath);
            return temp.ToString();
        }
        /// ﹤summary﹥  
        /// 验证文件是否存在  
        /// ﹤/summary﹥  
        /// ﹤returns﹥布尔值﹤/returns﹥  
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    }
    #endregion

    #region static class DstViewer
    public static class DstViewer
    {
        static readonly byte[] Encode = new byte[]
        {
            0x8C, 0x8F, 0x8E, 0x89, 0x88, 0x8B, 0x8A, 0x85, 0x84, 0x87, 0x86, 0x81, 0x80, 0x83, 0x82, 0xBD,
            0xBC, 0xBF, 0xBE, 0xB9, 0xB8, 0xBB, 0xBA, 0xB5, 0xB4, 0xB7, 0xB6, 0xB1, 0xB0, 0xB3, 0xB2, 0xAD,
            0xAC, 0xAF, 0xAE, 0xA9, 0xA8, 0xAB, 0xAA, 0xA5, 0xA4, 0xA7, 0xA6, 0xA1, 0xA0, 0xA3, 0xA2, 0xDD,
            0xDC, 0xDF, 0xDE, 0xD9, 0xD8, 0xDB, 0xDA, 0xD5, 0xD4, 0xD7, 0xD6, 0xD1, 0xD0, 0xD3, 0xD2, 0xCD,
            0xCC, 0xCF, 0xCE, 0xC9, 0xC8, 0xCB, 0xCA, 0xC5, 0xC4, 0xC7, 0xC6, 0xC1, 0xC0, 0xC3, 0xC2, 0xFD,
            0xFC, 0xFF, 0xFE, 0xF9, 0xF8, 0xFB, 0xFA, 0xF5, 0xF4, 0xF7, 0xF6, 0xF1, 0xF0, 0xF3, 0xF2, 0xED,
            0xEC, 0xEF, 0xEE, 0xE9, 0xE8, 0xEB, 0xEA, 0xE5, 0xE4, 0xE7, 0xE6, 0xE1, 0xE0, 0xE3, 0xE2, 0x1D,
            0x1C, 0x1F, 0x1E, 0x19, 0x18, 0x1B, 0x1A, 0x15, 0x14, 0x17, 0x16, 0x11, 0x10, 0x13, 0x12, 0xD,
            0xC,  0xF,  0xE,  0x9,  0x8,  0xB,  0xA,  0x5,  0x4,  0x7,  0x6,  0x1,  0x0,  0x3,  0x2,  0x3D,
            0x3C, 0x3F, 0x3E, 0x39, 0x38, 0x3B, 0x3A, 0x35, 0x34, 0x37, 0x36, 0x31, 0x30, 0x33, 0x32, 0x2D,
            0x2C, 0x2F, 0x2E, 0x29, 0x28, 0x2B, 0x2A, 0x25, 0x24, 0x27, 0x26, 0x21, 0x20, 0x23, 0x22, 0x5D,
            0x5C, 0x5F, 0x5E, 0x59, 0x58, 0x5B, 0x5A, 0x55, 0x54, 0x57, 0x56, 0x51, 0x50, 0x53, 0x52, 0x4D,
            0x4C, 0x4F, 0x4E, 0x49, 0x48, 0x4B, 0x4A, 0x45, 0x44, 0x47, 0x46, 0x41, 0x40, 0x43, 0x42, 0x7D,
            0x7C, 0x7F, 0x7E, 0x79, 0x78, 0x7B, 0x7A, 0x75, 0x74, 0x77, 0x76, 0x71, 0x70, 0x73, 0x72, 0x6D,
            0x6C, 0x6F, 0x6E, 0x69, 0x68, 0x6B, 0x6A, 0x65, 0x64, 0x67, 0x66, 0x61, 0x60, 0x63, 0x62, 0x9D,
            0x9C, 0x9F, 0x9E, 0x99, 0x98, 0x9B, 0x9A, 0x95, 0x94, 0x97, 0x96, 0x91, 0x90, 0x93, 0x92, 0x8D
        };
        static readonly byte[] Decode = new byte[]
        {
            0x8c, 0x8b, 0x8e, 0x8d, 0x88, 0x87, 0x8a, 0x89, 0x84, 0x83, 0x86, 0x85, 0x80, 0x7f, 0x82, 0x81,
            0x7c, 0x7b, 0x7e, 0x7d, 0x78, 0x77, 0x7a, 0x79, 0x74, 0x73, 0x76, 0x75, 0x70, 0x6f, 0x72, 0x71,
            0xac, 0xab, 0xae, 0xad, 0xa8, 0xa7, 0xaa, 0xa9, 0xa4, 0xa3, 0xa6, 0xa5, 0xa0, 0x9f, 0xa2, 0xa1,
            0x9c, 0x9b, 0x9e, 0x9d, 0x98, 0x97, 0x9a, 0x99, 0x94, 0x93, 0x96, 0x95, 0x90, 0x8f, 0x92, 0x91,
            0xcc, 0xcb, 0xce, 0xcd, 0xc8, 0xc7, 0xca, 0xc9, 0xc4, 0xc3, 0xc6, 0xc5, 0xc0, 0xbf, 0xc2, 0xc1,
            0xbc, 0xbb, 0xbe, 0xbd, 0xb8, 0xb7, 0xba, 0xb9, 0xb4, 0xb3, 0xb6, 0xb5, 0xb0, 0xaf, 0xb2, 0xb1,
            0xec, 0xeb, 0xee, 0xed, 0xe8, 0xe7, 0xea, 0xe9, 0xe4, 0xe3, 0xe6, 0xe5, 0xe0, 0xdf, 0xe2, 0xe1,
            0xdc, 0xdb, 0xde, 0xdd, 0xd8, 0xd7, 0xda, 0xd9, 0xd4, 0xd3, 0xd6, 0xd5, 0xd0, 0xcf, 0xd2, 0xd1,
            0xc,  0xb,  0xe,  0xd,  0x8,  0x7,  0xa,  0x9,  0x4,  0x3,  0x6,  0x5,  0x0,  0xff,  0x2,  0x1,
            0xfc, 0xfb, 0xfe, 0xfd, 0xf8, 0xf7, 0xfa, 0xf9, 0xf4, 0xf3, 0xf6, 0xf5, 0xf0, 0xef, 0xf2, 0xf1,
            0x2c, 0x2b, 0x2e, 0x2d, 0x28, 0x27, 0x2a, 0x29, 0x24, 0x23, 0x26, 0x25, 0x20, 0x1f, 0x22, 0x21,
            0x1c, 0x1b, 0x1e, 0x1d, 0x18, 0x17, 0x1a, 0x19, 0x14, 0x13, 0x16, 0x15, 0x10, 0xf,  0x12, 0x11,
            0x4c, 0x4b, 0x4e, 0x4d, 0x48, 0x47, 0x4a, 0x49, 0x44, 0x43, 0x46, 0x45, 0x40, 0x3f, 0x42, 0x41,
            0x3c, 0x3b, 0x3e, 0x3d, 0x38, 0x37, 0x3a, 0x39, 0x34, 0x33, 0x36, 0x35, 0x30, 0x2f, 0x32, 0x31,
            0x6c, 0x6b, 0x6e, 0x6d, 0x68, 0x67, 0x6a, 0x69, 0x64, 0x63, 0x66, 0x65, 0x60, 0x5f, 0x62, 0x61,
            0x5c, 0x5b, 0x5e, 0x5d, 0x58, 0x57, 0x5a, 0x59, 0x54, 0x53, 0x56, 0x55, 0x50, 0x4f, 0x52, 0x51
        };
        private static byte[] DecryptFile(IEnumerable<byte> bytes)
        {
            return bytes.Select(b => Decode[b]).ToArray();
        }
        private static byte[] EncryptFile(IEnumerable<byte> bytes)
        {
            return bytes.Select(b => Encode[b]).ToArray();
        }
        public static void DstToXmlFile(string dstfile,string xmlfile)
        {
            if (File.Exists(dstfile) == false) 
            {
                throw new FileNotFoundException();
            }
            byte[] XmlBytes = File.ReadAllBytes(Environment.ExpandEnvironmentVariables(dstfile));
            MemoryStream Ms = new MemoryStream(DecryptFile(XmlBytes));
            XmlDocument Xml = new XmlDocument();
            Xml.Load(Ms);
            Xml.Save(xmlfile);
        }
        public static XmlDocument DstToXml(string dstfile)
        {
            XmlDocument Xml = new XmlDocument();
            if (File.Exists(dstfile) == false)
            {
                throw new FileNotFoundException();
            }
            byte[] XmlBytes = File.ReadAllBytes(Environment.ExpandEnvironmentVariables(dstfile));
            MemoryStream Ms = new MemoryStream(DecryptFile(XmlBytes));
            Xml.Load(Ms);
            return Xml;
        }
        public static void XmlFileToDst(string xmlfile,string dstfile)
        {
            if (File.Exists(xmlfile) == false)
            {
                throw new FileNotFoundException();
            }
            XmlDocument Xml = new XmlDocument();
            Xml.Load(xmlfile);
            MemoryStream Ms = new MemoryStream();
            Xml.Save(Ms);
            File.WriteAllBytes(dstfile, EncryptFile(Ms.ToArray()));
        }
        public static void XmlToDst(XmlDocument xml,string dstfile)
        {
            MemoryStream Ms = new MemoryStream();
            xml.Save(Ms);
            File.WriteAllBytes(dstfile, EncryptFile(Ms.ToArray()));
        }
    }
    #endregion
}
