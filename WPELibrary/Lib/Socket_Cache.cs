﻿using System;
using System.Data;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using System.Reflection;

namespace WPELibrary.Lib
{
    public static class Socket_Cache
    {
        public static byte[] bByteBuff = new byte[0];
        public static bool Hook_Send, Hook_SendTo, Hook_Recv, Hook_RecvFrom, Hook_WSASend, Hook_WSASendTo, Hook_WSARecv, Hook_WSARecvFrom;
        public static bool Check_Size, Check_Socket, Check_IP, Check_Packet;
        public static string txtCheck_Socket, txtCheck_IP, txtCheck_Packet;
        public static decimal txtCheck_Size_From, txtCheck_Size_To;

        #region//封包队列
        public static class SocketQueue
        {  
            public static int Filter_CNT = 0;
            public static int Recv_CNT = 0;
            public static int Send_CNT = 0;

            public static Queue<Socket_Packet> qSocket_Packet = new Queue<Socket_Packet>();

            #region//封包入队列

            public static void SocketPacketToQueue(int iSocket, byte[] bBuffByte, Socket_Packet.SocketType sType, Socket_Packet.sockaddr sAddr)
            {
                try
                {
                    Socket_Packet sp = new Socket_Packet(iSocket, IntPtr.Zero, bBuffByte.Length, sType, sAddr, bBuffByte, bBuffByte.Length);

                    if (Socket_Operation.ISShowSocketPacket_ByFilter(sp))
                    {
                        lock (qSocket_Packet)
                        {
                            qSocket_Packet.Enqueue(sp);
                        }
                    }
                    else
                    {
                        Filter_CNT++;
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }

            public static void SocketPacketToQueue(int iSocket, IntPtr ipBuff, int iLen, Socket_Packet.SocketType sType, Socket_Packet.sockaddr sAddr, int iResLen)
            {
                try
                {
                    byte[] bBuffer = new byte[iResLen];
                    Marshal.Copy(ipBuff, bBuffer, 0, iResLen);

                    Socket_Packet sp = new Socket_Packet(iSocket, ipBuff, iLen, sType, sAddr, bBuffer, iResLen);

                    if (Socket_Operation.ISShowSocketPacket_ByFilter(sp))
                    {
                        lock (qSocket_Packet)
                        {
                            qSocket_Packet.Enqueue(sp);
                        }
                    }
                    else
                    {
                        Filter_CNT++;
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }                
            }

            #endregion

            #region//清除队列数据
            public static void ResetSocketQueue()
            {
                try
                {                    
                    Filter_CNT = 0;
                    Recv_CNT = 0;
                    Send_CNT = 0;

                    qSocket_Packet.Clear();
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            #endregion
        }
        #endregion

        #region//封包列表
        public static class SocketList
        {
            public static BindingList<Socket_Packet_Info> lstRecPacket = new BindingList<Socket_Packet_Info>();

            public delegate void SocketPacketReceived(Socket_Packet_Info si);
            public static event SocketPacketReceived RecSocketPacket;

            #region//封包入列表
            public static void SocketToList(int iMax_DataLen)
            {
                try
                {
                    if (SocketQueue.qSocket_Packet.Count > 0)
                    {
                        Socket_Packet sa = SocketQueue.qSocket_Packet.Dequeue();

                        int iIndex = lstRecPacket.Count + 1;
                        Socket_Packet.SocketType sType = sa.Type;
                        int iSocket = sa.Socket;
                        int iResLen = sa.ResLen;
                        byte[] bBuffer = sa.Buffer;

                        string sData = "";

                        if (iResLen > iMax_DataLen)
                        {
                            byte[] bTemp = new byte[iMax_DataLen];

                            for (int j = 0; j < iMax_DataLen; j++)
                            {
                                bTemp[j] = bBuffer[j];
                            }

                            sData = Socket_Operation.ByteToString("HEX", bTemp) + " ...";
                        }
                        else
                        {
                            sData = Socket_Operation.ByteToString("HEX", bBuffer);
                        }

                        Socket_Packet.sockaddr sAddr = sa.Addr;

                        string sIP_From = "", sIP_To = "";

                        switch (sType)
                        {  
                            case Socket_Packet.SocketType.Send:
                                SocketQueue.Send_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                sIP_To = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.To);
                                break;
                            case Socket_Packet.SocketType.SendTo:
                                SocketQueue.Send_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                sIP_To = Socket_Operation.GetSocketIP(sAddr.sin_addr, sAddr.sin_port);
                                break;
                            case Socket_Packet.SocketType.Recv:
                                SocketQueue.Recv_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.To);
                                sIP_To = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                break;
                            case Socket_Packet.SocketType.RecvFrom:
                                SocketQueue.Recv_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(sAddr.sin_addr, sAddr.sin_port);
                                sIP_To = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                break;
                            case Socket_Packet.SocketType.WSASend:
                                SocketQueue.Send_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                sIP_To = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.To);
                                break;
                            case Socket_Packet.SocketType.WSASendTo:
                                SocketQueue.Send_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                sIP_To = Socket_Operation.GetSocketIP(sAddr.sin_addr, sAddr.sin_port);
                                break;
                            case Socket_Packet.SocketType.WSARecv:
                                SocketQueue.Recv_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.To);
                                sIP_To = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                break;
                            case Socket_Packet.SocketType.WSARecvFrom:
                                SocketQueue.Recv_CNT++;
                                sIP_From = Socket_Operation.GetSocketIP(sAddr.sin_addr, sAddr.sin_port);
                                sIP_To = Socket_Operation.GetSocketIP(iSocket, Socket_Packet.IPType.From);
                                break;
                        }                     

                        Socket_Packet_Info spi = new Socket_Packet_Info(iIndex, sType, iSocket, sIP_From, sIP_To, iResLen, sData, bBuffer);
                        RecSocketPacket?.Invoke(spi);
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            #endregion
        }
        #endregion

        #region//日志队列
        public static class LogQueue
        {
            public static Queue<Socket_Log_Info> qSocket_Log = new Queue<Socket_Log_Info>();

            #region//日志入队列

            public static void LogToQueue(string sFuncName, string sLogContent)
            {
                try
                {
                    Socket_Log_Info sli = new Socket_Log_Info(sFuncName, sLogContent);

                    lock (qSocket_Log)
                    {
                        qSocket_Log.Enqueue(sli);
                    }                 
                }
                catch (Exception ex) 
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }            

            #endregion

            #region//清除队列数据
            public static void ResetLogQueue()
            {
                try
                {
                    qSocket_Log.Clear();
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            #endregion
        }
        #endregion

        #region//日志列表
        public static class LogList
        {
            public static BindingList<Socket_Log_Info> lstRecLog = new BindingList<Socket_Log_Info>();

            public delegate void SocketLogReceived(Socket_Log_Info sl);
            public static event SocketLogReceived RecSocketLog;

            #region//日志入列表
            public static void LogToList()
            {
                try
                {
                    if (LogQueue.qSocket_Log.Count > 0)
                    {
                        Socket_Log_Info sli = LogQueue.qSocket_Log.Dequeue();
                        RecSocketLog?.Invoke(sli);
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            #endregion
        }
        #endregion

        #region//滤镜列表
        public static class SocketFilterList
        {
            public static int SearchRowIndex = 0;
            public static int ModifyRowIndex = 1;
            public static int FilterLen_MAX = 100;
            public static BindingList<Socket_Filter_Info> lstFilter = new BindingList<Socket_Filter_Info>();

            #region//初始化滤镜列表
            public static void InitFilterList(int iFilterMaxNum)
            {
                try
                {
                    lstFilter.Clear();

                    for (int i = 0; i < iFilterMaxNum; i++)
                    {                        
                        AddFilter_New();
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            #endregion

            #region//清空滤镜列表

            public static void FilterListClear()
            {
                try
                {
                    lstFilter.Clear();
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }

            #endregion

            #region//设置滤镜是否启用

            public static void SetIsCheck_ByFilterNum(int FNum, bool bCheck)
            {
                try
                {
                    if (FNum > 0)
                    {
                        int iFIndex = GetFilterIndex_ByFilterNum(FNum);

                        lstFilter[iFIndex].IsCheck = bCheck;
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }

            #endregion

            #region//获取滤镜是否启用

            public static bool GetIsCheck_ByFilterNum(int FNum)
            {
                bool bReturn = false;

                try
                {
                    if (FNum > 0)
                    {
                        int iFIndex = GetFilterIndex_ByFilterNum(FNum);

                        bReturn = lstFilter[iFIndex].IsCheck;
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                return bReturn;                
            }

            #endregion

            #region//返回滤镜长度

            public static int GetFilterLen_ByFilterNum(int FNum)
            {
                int iReturn = 0;

                try
                {
                    if (FNum > 0)
                    {
                        int iFIndex = GetFilterIndex_ByFilterNum(FNum);

                        string sFSearch = lstFilter[iFIndex].FSearch;
                        string sFModify = lstFilter[iFIndex].FModify;


                        int iFSearch = 0, iFModify = 0;

                        if (string.IsNullOrEmpty(sFSearch) && string.IsNullOrEmpty(sFModify))
                        {
                            iReturn = FilterLen_MAX;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(sFSearch))
                            {
                                string[] slFSearch = sFSearch.Split(',');
                                string[] slTemp = slFSearch[slFSearch.Length - 1].ToString().Split('-');
                                iFSearch = int.Parse(slTemp[0].ToString());
                            }

                            if (!string.IsNullOrEmpty(sFModify))
                            {
                                string[] slFModify = sFModify.Split(',');
                                string[] slTemp = slFModify[slFModify.Length - 1].ToString().Split('-');
                                iFModify = int.Parse(slTemp[0].ToString());
                            }

                            if (iFSearch >= iFModify)
                            {
                                iReturn = iFSearch + 1;
                            }
                            else
                            {
                                iReturn = iFModify + 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                return iReturn;
            }

            #endregion

            #region//滤镜列表操作（新增，修改，删除）

            //新增滤镜
            public static void AddFilter_New()
            {
                try
                {
                    AddFilter_New("", "", "", false);
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            
            public static void AddFilter_New(string FName, string FSearch, string FModify, bool bCheck)
            {
                try
                {
                    int FNum = GetFilterNum_New();

                    if (string.IsNullOrEmpty(FName))
                    {
                        FName = MultiLanguage.GetDefaultLanguage(MultiLanguage.MutiLan_50) + " " + FNum.ToString();
                    }

                    Socket_Filter_Info sc = new Socket_Filter_Info(FNum, bCheck, FName, FSearch, FModify);

                    lstFilter.Add(sc);
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }
            
            private static int GetFilterNum_New()
            {
                int iReturn = 0;

                try
                {
                    for (int i = 0; i < lstFilter.Count; i++)
                    {
                        int iFNum = lstFilter[i].FNum;

                        if (iFNum > iReturn)
                        {
                            iReturn = iFNum;
                        }
                    }

                    iReturn = iReturn + 1;
                }
                catch (Exception ex)
                {
                    iReturn = 0;

                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }                

                return iReturn;
            }

            //删除滤镜
            public static void DeleteFilter_ByFilterNum(int FNum)
            {
                try
                {
                    if (FNum > 0)
                    {
                        int iFIndex = GetFilterIndex_ByFilterNum(FNum);

                        if (iFIndex > -1)
                        {
                            lstFilter.RemoveAt(iFIndex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }

            //修改滤镜
            public static void UpdateFilter_ByFilterNum(int FNum, string FName, string FSearch, string FModify)
            {
                try
                {
                    if (FNum > 0 && !string.IsNullOrEmpty(FName))
                    {
                        int iFIndex = GetFilterIndex_ByFilterNum(FNum);

                        if (iFIndex > -1)
                        {
                            lstFilter[iFIndex].FName = FName;
                            lstFilter[iFIndex].FSearch = FSearch;
                            lstFilter[iFIndex].FModify = FModify;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }
            }

            //获取滤镜序号
            public static int GetFilterIndex_ByFilterNum(int FNum)
            {
                int iReturn = -1;

                try
                {
                    for (int i = 0; i < lstFilter.Count; i++)
                    {
                        int iFNum = lstFilter[i].FNum;

                        if (iFNum == FNum)
                        {
                            iReturn = i;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    iReturn = -1;

                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                return iReturn;
            }

            //获取滤镜编号
            public static int GetFilterNum_ByFilterIndex(int FIndex)
            {
                int iReturn = -1;

                try
                {
                    int iFNum = lstFilter[FIndex].FNum;

                    if (iFNum > 0)
                    {
                        iReturn = iFNum;
                    }
                }
                catch (Exception ex)
                {
                    iReturn = -1;

                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                return iReturn;
            }

            #endregion

            #region//执行滤镜
            public static void DoFilter(IntPtr ipBuff, int iLen)
            {
                int iFNum = 0;
                string sFName = "", sFSearch = "", sModify = "";

                try
                {
                    byte[] bBuff = Socket_Operation.GetByteFromIntPtr(ipBuff, iLen);

                    if (bBuff.Length > 0)
                    {
                        foreach (Socket_Filter_Info sfi in lstFilter)
                        {
                            bool bCheck = sfi.IsCheck;

                            if (bCheck)
                            {
                                iFNum = sfi.FNum;
                                sFName = sfi.FName;
                                sFSearch = sfi.FSearch;
                                sModify = sfi.FModify;

                                if (!string.IsNullOrEmpty(sFSearch) && !string.IsNullOrEmpty(sModify))
                                {
                                    if (CheckFilterSearch_ByBuff(sFSearch, bBuff))
                                    {
                                        string[] ssModify = sModify.Split(',');

                                        foreach (string sTemp in ssModify)
                                        {
                                            string[] sModifyValue = sTemp.Split('-');
                                            int iIndex = int.Parse(sModifyValue[0].ToString().Trim());
                                            string sValue = sModifyValue[1].ToString().Trim();

                                            bBuff[iIndex] = Socket_Operation.Hex_To_Byte(sValue)[0];
                                        }

                                        bool bSetOK = Socket_Operation.SetByteToIntPtr(bBuff, ipBuff, iLen);

                                        if (bSetOK)
                                        {                                            
                                            Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, MultiLanguage.GetDefaultLanguage(MultiLanguage.MutiLan_51) + sFName);

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {                    
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, MultiLanguage.GetDefaultLanguage(MultiLanguage.MutiLan_52) + sFName + " | " + ex.Message);
                }
            }
            #endregion

            #region//检查是否匹配滤镜
            private static bool CheckFilterSearch_ByBuff(string FSearch, byte[] bBuff)
            {
                bool bResult = true;

                try
                {
                    if (!string.IsNullOrEmpty(FSearch))
                    {
                        string[] ssSearch = FSearch.Split(',');

                        foreach (string sSearch in ssSearch)
                        {
                            string[] sSearchValue = sSearch.Split('-');
                            int iIndex = int.Parse(sSearchValue[0].ToString().Trim());
                            string sValue = sSearchValue[1].ToString().Trim();

                            string sBufferValue = bBuff[iIndex].ToString("X2");

                            if (!sValue.Equals(sBufferValue))
                            {
                                bResult = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        bResult = false;
                    }
                }
                catch (Exception ex)
                {                    
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, MultiLanguage.GetDefaultLanguage(MultiLanguage.MutiLan_53) + ex.Message);

                    bResult = false;
                }

                return bResult;
            }
            #endregion
        }
        #endregion

        #region//封包发送列表
        public static class SocketSendList
        {
            public static int Loop_CNT = 0;//循环次数
            public static int Loop_Int = 0;//循环间隔
            public static int Loop_Send_CNT = 0;//已循环次数
            public static int SendList_Success_CNT = 0;//发送成功
            public static int SendList_Fail_CNT = 0;//发送失败
            public static int UseSocket = 0;//使用此套接字
            public static bool bShow_SendListForm = true;

            public static DataTable dtSocketSendList = new DataTable();

            #region//初始化发送列表
            public static void InitSendList()
            {
                dtSocketSendList.Columns.Clear();

                dtSocketSendList.Columns.Add("ID", typeof(int));
                dtSocketSendList.Columns.Add("Remark", typeof(string));
                dtSocketSendList.Columns.Add("Socket", typeof(int));
                dtSocketSendList.Columns.Add("ToAddress", typeof(string));
                dtSocketSendList.Columns.Add("Len", typeof(int));
                dtSocketSendList.Columns.Add("Data", typeof(string));
                dtSocketSendList.Columns.Add("Bytes", typeof(byte[]));
            }
            #endregion            

            #region//发送列表操作（新增，删除）
            public static void AddSendList_BySocketListIndex(int iSLIndex)
            {
                AddSendList_New(
                    SocketList.lstRecPacket[iSLIndex].Index,
                    "",
                    SocketList.lstRecPacket[iSLIndex].Socket,
                    SocketList.lstRecPacket[iSLIndex].To,
                    SocketList.lstRecPacket[iSLIndex].ResLen,
                    SocketList.lstRecPacket[iSLIndex].Data,
                    SocketList.lstRecPacket[iSLIndex].Buffer
                    );
            }

            public static void AddSendList_New(int iIndex, string sNote, int iSocket, string sIPTo, int iResLen, string sData, byte[] bBuffer)
            {
                DataRow dr = dtSocketSendList.NewRow();

                dr["ID"] = iIndex;
                dr["Remark"] = sNote;
                dr["Socket"] = iSocket;
                dr["ToAddress"] = sIPTo;
                dr["Len"] = iResLen;
                dr["Data"] = sData;
                dr["Bytes"] = bBuffer;
                dtSocketSendList.Rows.Add(dr);
            }

            public static void DeleteSendList_ByIndex(int SIndex)
            {
                dtSocketSendList.Rows[SIndex].Delete();
            }
            #endregion

            #region//清空发送列表
            public static void SendListClear()
            {
                dtSocketSendList.Rows.Clear();
            }
            #endregion

            #region//发送列表
            public static bool SendPacketList_ByIndex(int iSocket, int iIndex)
            {
                bool bResult = false;

                try
                {
                    int iLen = (int)dtSocketSendList.Rows[iIndex]["Len"];
                    byte[] bBuffer = (byte[])dtSocketSendList.Rows[iIndex]["Bytes"];

                    if (bBuffer.Length > 0)
                    {
                        IntPtr ipSend = Marshal.AllocHGlobal(bBuffer.Length);
                        Marshal.Copy(bBuffer, 0, ipSend, bBuffer.Length);

                        bool bReturn = WinSockHook.SendPacket(iSocket, ipSend, bBuffer.Length);

                        if (bReturn)
                        {
                            SendList_Success_CNT++;
                        }
                        else
                        {
                            SendList_Fail_CNT++;
                        }

                        Thread.Sleep(Loop_Int);
                    }
                }
                catch (Exception ex)
                {
                    Socket_Operation.DoLog(MethodBase.GetCurrentMethod().Name, ex.Message);
                }

                return bResult;
            }
            #endregion
        }
        #endregion
    }
}
