﻿using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
#if !NETSTANDARD2_0
using System.IO.Ports;
#endif
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Profinet.LSIS
{
    /// <summary>
    /// LSisServer
    /// </summary>
    public class LSisServer : NetworkDataServerBase
    {
        #region Constructor

        /// <summary>
        /// LSisServer  
        /// </summary>
        public LSisServer( )
        {

            pBuffer = new SoftBuffer( DataPoolLength );
            qBuffer = new SoftBuffer( DataPoolLength );
            mBuffer = new SoftBuffer( DataPoolLength );
            dBuffer = new SoftBuffer( DataPoolLength );

            WordLength = 2;
            ByteTransform = new RegularByteTransform( );

#if !NETSTANDARD2_0
            serialPort = new SerialPort( );
#endif
        }

        #endregion

        #region NetworkDataServerBase Override

        /// <summary>
        /// 读取自定义的寄存器的值
        /// </summary>
        /// <param name="address">起始地址，示例："I100"，"M100"</param>
        /// <param name="length">数据长度</param>
        /// <exception cref="IndexOutOfRangeException"></exception>
        /// <returns>byte数组值</returns>
        public override OperateResult<byte[]> Read( string address, ushort length )
        {
            OperateResult<string> analysis = XGBFastEnet.AnalysisAddress( address, true );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

            int startIndex = int.Parse( analysis.Content.Substring( 3 ) );
            switch (analysis.Content[1])
            {
                case 'P': return OperateResult.CreateSuccessResult( pBuffer.GetBytes( startIndex, length ) );
                case 'Q': return OperateResult.CreateSuccessResult( qBuffer.GetBytes( startIndex, length ) );
                case 'M': return OperateResult.CreateSuccessResult( mBuffer.GetBytes( startIndex, length ) );
                case 'D': return OperateResult.CreateSuccessResult( dBuffer.GetBytes( startIndex, length ) );
                default: return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
            }
        }

        /// <summary>
        /// 写入自定义的数据到数据内存中去
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="value">数据值</param>
        /// <returns>是否写入成功的结果对象</returns>
        public override OperateResult Write( string address, byte[] value )
        {
            OperateResult<string> analysis = XGBFastEnet.AnalysisAddress( address, false );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

            int startIndex = int.Parse( analysis.Content.Substring( 3 ) );
            switch (analysis.Content[1])
            {
                case 'P': pBuffer.SetBytes( value, startIndex ); return OperateResult.CreateSuccessResult( );
                case 'Q': qBuffer.SetBytes( value, startIndex ); return OperateResult.CreateSuccessResult( );
                case 'M': mBuffer.SetBytes( value, startIndex ); return OperateResult.CreateSuccessResult( );
                case 'D': dBuffer.SetBytes( value, startIndex ); return OperateResult.CreateSuccessResult( );
                default: return new OperateResult<byte[]>( StringResources.Language.NotSupportedDataType );
            }
        }

        #endregion

        #region Byte Read Write Operate

        /// <summary>
        /// 读取指定地址的字节数据
        /// </summary>
        /// <param name="address">西门子的地址信息</param>
        /// <returns>带有成功标志的结果对象</returns>
        public OperateResult<byte> ReadByte( string address )
        {
            OperateResult<byte[]> read = Read( address, 2 );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte>( read );

            return OperateResult.CreateSuccessResult( read.Content[0] );
        }

        /// <summary>
        /// 将byte数据信息写入到指定的地址当中
        /// </summary>
        /// <param name="address">西门子的地址信息</param>
        /// <param name="value">字节数据信息</param>
        /// <returns>是否成功的结果</returns>
        public OperateResult Write( string address, byte value )
        {
            return Write( address, new byte[] { value } );
        }

        #endregion

        #region Bool Read Write Operate

        /// <summary>
        /// 读取指定地址的bool数据对象
        /// </summary>
        /// <param name="address">西门子的地址信息</param>
        /// <returns>带有成功标志的结果对象</returns>
        public OperateResult<bool> ReadBool( string address )
        {
            OperateResult<string> analysis = XGBFastEnet.AnalysisAddress( address, true );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<bool>( analysis );

            // to do, this is not right
            int startIndex = int.Parse( analysis.Content.Remove( 0, 3 ) );
            switch (analysis.Content[1])
            {
                case 'P': return OperateResult.CreateSuccessResult( pBuffer.GetBool( startIndex ) );
                case 'Q': return OperateResult.CreateSuccessResult( qBuffer.GetBool( startIndex ) );
                case 'M': return OperateResult.CreateSuccessResult( mBuffer.GetBool( startIndex ) );
                case 'D': return OperateResult.CreateSuccessResult( dBuffer.GetBool( startIndex ) );
                default: return new OperateResult<bool>( StringResources.Language.NotSupportedDataType );
            }
        }

        /// <summary>
        /// 往指定的地址里写入bool数据对象
        /// </summary>
        /// <param name="address">西门子的地址信息</param>
        /// <param name="value">值</param>
        /// <returns>是否成功的结果</returns>
        public OperateResult Write( string address, bool value )
        {
            OperateResult<string> analysis = XGBFastEnet.AnalysisAddress( address, false );
            if (!analysis.IsSuccess) return analysis;

            // to do, this is not right
            int startIndex = int.Parse( analysis.Content.Remove( 0, 3 ) );
            switch (analysis.Content[1])
            {
                case 'P': pBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
                case 'Q': qBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
                case 'M': mBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
                case 'D': dBuffer.SetBool( value, startIndex ); return OperateResult.CreateSuccessResult( );
                default: return new OperateResult( StringResources.Language.NotSupportedDataType );
            }
        }

        #endregion

        #region NetServer Override

        /// <summary>
        /// 当客户端登录后，进行Ip信息的过滤，然后触发本方法，也就是说之后的客户端需要
        /// </summary>
        /// <param name="socket">网络套接字</param>
        /// <param name="endPoint">终端节点</param>
        protected override void ThreadPoolLoginAfterClientCheck( Socket socket, System.Net.IPEndPoint endPoint )
        {
            // 开始接收数据信息
            AppSession appSession = new AppSession( );
            appSession.IpEndPoint = endPoint;
            appSession.WorkSocket = socket;
            try
            {
                socket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), appSession );
                AddClient( appSession );
            }
            catch
            {
                socket.Close( );
                LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, endPoint ) );
            }
        }

        private void SocketAsyncCallBack( IAsyncResult ar )
        {
            if (ar.AsyncState is AppSession session)
            {
                try
                {
                    int receiveCount = session.WorkSocket.EndReceive( ar );

                    LsisFastEnetMessage fastEnetMessage = new LsisFastEnetMessage( );
                    OperateResult<byte[]> read1 = ReceiveByMessage( session.WorkSocket, 5000, fastEnetMessage );
                    if (!read1.IsSuccess)
                    {
                        LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
                        RemoveClient( session );
                        return;
                    };

                    byte[] receive = read1.Content;

                    if (receive[20] == 0x54)
                    {
                        // 读数据
                        session.WorkSocket.Send( ReadByMessage( receive ) );
                    }
                    else if (receive[20] == 0x58)
                    {
                        // 写数据
                        session.WorkSocket.Send( WriteByMessage( receive ) );
                    }
                    else
                    {
                        session.WorkSocket.Close( );
                    }

                    RaiseDataReceived( receive );
                    session.WorkSocket.BeginReceive( new byte[0], 0, 0, SocketFlags.None, new AsyncCallback( SocketAsyncCallBack ), session );
                }
                catch
                {
                    // 关闭连接，记录日志
                    session.WorkSocket?.Close( );
                    LogNet?.WriteDebug( ToString( ), string.Format( StringResources.Language.ClientOfflineInfo, session.IpEndPoint ) );
                    RemoveClient( session );
                    return;
                }
            }
        }

        private byte[] ReadByMessage( byte[] packCommand )
        {
            List<byte> content = new List<byte>( );

            content.AddRange( ReadByCommand( packCommand ) );


            return content.ToArray( );
        }

        private byte[] ReadByCommand( byte[] command )
        {
            var result = new List<byte>( );

            result.AddRange( SoftBasic.BytesArraySelectBegin( command, 20 ) );
            result[ 9] = 0x11;
            result[10] = 0x01;
            result[12] = 0xA0;
            result[13] = 0x11;
            result[18] = 0x03;
            result.AddRange( new byte[] { 0x55, 0x00, 0x14, 0x00, 0x08, 0x01, 0x00, 0x00, 0x01, 0x00 } );

            int NameLength = command[28];
            ushort RequestCount = BitConverter.ToUInt16( command, 30 + NameLength );

            string DeviceAddress = Encoding.ASCII.GetString( command, 31, NameLength - 1 );
            byte[] data = Read( DeviceAddress, RequestCount ).Content;

            result.AddRange( BitConverter.GetBytes( (ushort)data.Length ) );
            result.AddRange( data );
            result[16] = (byte)(result.Count - 20);
            return result.ToArray( );
        }



        private byte[] WriteByMessage( byte[] packCommand )
        {
            var result = new List<byte>( );

            result.AddRange( SoftBasic.BytesArraySelectBegin( packCommand, 20 ) );
            result[ 9] = 0x11;
            result[10] = 0x01;
            result[12] = 0xA0;
            result[13] = 0x11;
            result[18] = 0x03;
            result.AddRange( new byte[] { 0x59, 0x00, 0x14, 0x00, 0x08, 0x01, 0x00, 0x00, 0x01, 0x00 } );

            int NameLength = packCommand[28];
            var DeviceAddress = Encoding.ASCII.GetString( packCommand, 31, NameLength - 1 );
            int RequestCount = BitConverter.ToUInt16( packCommand, 30 + NameLength );

            byte[] data = ByteTransform.TransByte( packCommand, 32 + NameLength, RequestCount );
            Write( DeviceAddress, data );
            return result.ToArray( );
        }

        #endregion

        #region Data Save Load Override

        /// <summary>
        /// 从字节数据加载数据信息
        /// </summary>
        /// <param name="content">字节数据</param>
        protected override void LoadFromBytes( byte[] content )
        {
            if (content.Length < DataPoolLength * 4) throw new Exception( "File is not correct" );

            pBuffer.SetBytes( content, 0, 0, DataPoolLength );
            qBuffer.SetBytes( content, DataPoolLength, 0, DataPoolLength );
            mBuffer.SetBytes( content, DataPoolLength * 2, 0, DataPoolLength );
            dBuffer.SetBytes( content, DataPoolLength * 3, 0, DataPoolLength );
        }

        /// <summary>
        /// 将数据信息存储到字节数组去
        /// </summary>
        /// <returns>所有的内容</returns>
        protected override byte[] SaveToBytes( )
        {
            byte[] buffer = new byte[DataPoolLength * 4];
            Array.Copy( pBuffer.GetBytes( ), 0, buffer, 0, DataPoolLength );
            Array.Copy( qBuffer.GetBytes( ), 0, buffer, DataPoolLength, DataPoolLength );
            Array.Copy( mBuffer.GetBytes( ), 0, buffer, DataPoolLength * 2, DataPoolLength );
            Array.Copy( dBuffer.GetBytes( ), 0, buffer, DataPoolLength * 3, DataPoolLength );

            return buffer;
        }

        public static int CheckAddress( string address )
        {
            int bitSelacdetAddress;
            switch (address)
            {
                case "A":
                    bitSelacdetAddress = 10;
                    break;
                case "B":
                    bitSelacdetAddress = 11;
                    break;
                case "C":
                    bitSelacdetAddress = 12;
                    break;
                case "D":
                    bitSelacdetAddress = 13;
                    break;
                case "E":
                    bitSelacdetAddress = 14;
                    break;
                case "F":
                    bitSelacdetAddress = 15;
                    break;

                default:
                    bitSelacdetAddress = int.Parse( address );
                    break;
            }
            return bitSelacdetAddress;
        }

        #endregion

        #region Private Member

        private SoftBuffer pBuffer;                    // p data type
        private SoftBuffer qBuffer;                    // q data type
        private SoftBuffer mBuffer;                    // 寄存器的数据池
        private SoftBuffer dBuffer;                    // 输入寄存器的数据池
        private const int DataPoolLength = 65536;      // 数据的长度

        #endregion

        #region Serial Support
        private int station = 1;
#if !NETSTANDARD2_0

        private SerialPort serialPort;            // 核心的串口对象

        /// <summary>
        /// 使用默认的参数进行初始化串口，9600波特率，8位数据位，无奇偶校验，1位停止位
        /// </summary>
        /// <param name="com">串口信息</param>
        public void StartSerialPort( string com )
        {
            StartSerialPort( com, 9600 );
        }

        /// <summary>
        /// 使用默认的参数进行初始化串口，8位数据位，无奇偶校验，1位停止位
        /// </summary>
        /// <param name="com">串口信息</param>
        /// <param name="baudRate">波特率</param>
        public void StartSerialPort( string com, int baudRate )
        {
            StartSerialPort( sp =>
             {
                 sp.PortName = com;
                 sp.BaudRate = baudRate;
                 sp.DataBits = 8;
                 sp.Parity = Parity.None;
                 sp.StopBits = StopBits.One;
             } );
        }

        /// <summary>
        /// 使用自定义的初始化方法初始化串口的参数
        /// </summary>
        /// <param name="inni">初始化信息的委托</param>
        public void StartSerialPort( Action<SerialPort> inni )
        {
            if (!serialPort.IsOpen)
            {
                inni?.Invoke( serialPort );

                serialPort.ReadBufferSize = 1024;
                serialPort.ReceivedBytesThreshold = 1;
                serialPort.Open( );
                serialPort.DataReceived += SerialPort_DataReceived;
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void CloseSerialPort( )
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close( );
            }
        }
        private byte[] bufferReceiver;
        /// <summary>
        /// 接收到串口数据的时候触发
        /// </summary>
        /// <param name="sender">串口对象</param>
        /// <param name="e">消息</param>
        private void SerialPort_DataReceived( object sender, SerialDataReceivedEventArgs e )
        {
            var sp = (SerialPort)sender;
            if (sp.BytesToRead >= 5)
            {
                bufferReceiver = new byte[serialPort.BytesToRead];
                var result = serialPort.Read( bufferReceiver, 0, serialPort.BytesToRead );
                ProcessReceivedData( bufferReceiver );
            }




        }
        private readonly object LockObject = new object( );
        public bool ResponseReceived = false;
        private void ProcessReceivedData( byte[] receive )
        {
            byte[] modbusCore = SoftBasic.BytesArrayRemoveLast( receive, 2 );
            lock (LockObject)
            {
                var bufferMsgReceiver = string.Empty;

                if (receive == null || receive.Length < 1)
                {

                    ResponseReceived = true;
                }
                else
                {

                    bufferMsgReceiver = Encoding.UTF8.GetString( modbusCore, 0, modbusCore.Length );
                    byte[] copy = ReadFromModbusCore( modbusCore );

                    serialPort.Write( copy, 0, copy.Length );
                    if (IsStarted) RaiseDataReceived( receive );

                }
            }

        }
        public static string GetValStr( byte[] Buff, int iStart, int iDataSize )
        {
            var strVal = string.Empty;
            var strByteVal = string.Empty;
            var i = 0;

            for (i = 0; i < iDataSize; i++)
            {
                strByteVal = Convert.ToString( Buff[i + iStart], 16 ).ToUpper( );
                if (strByteVal.Length == 1) strByteVal = "0" + strByteVal;
                strVal = strByteVal + strVal;
            }

            return strVal;
        }
        public byte[] HexToBytes( string hex )
        {
            if (hex == null)
                throw new ArgumentNullException( "The data is null" );

            if (hex.Length % 2 != 0)
                throw new FormatException( "Hex Character Count Not Even" );

            var bytes = new byte[hex.Length / 2];

            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte( hex.Substring( i * 2, 2 ), 16 );

            return bytes;
        }
        private byte[] ReadFromModbusCore( byte[] packet )
        {
            List<byte> command = new List<byte>( );
            command.Clear( );
            var StartAddress = string.Empty;
            var station = Encoding.ASCII.GetString( packet, 1, 2 );
            var Read = Encoding.ASCII.GetString( packet, 3, 3 );
            var nameLength = Encoding.ASCII.GetString( packet, 6, 2 );
            var DeviceAddress = Encoding.ASCII.GetString( packet, 8, int.Parse( nameLength ) );
            var size = Encoding.ASCII.GetString( packet, 8 + int.Parse( nameLength ), 2 );
            //=====================================================================================
            // Read Response
            if (Read.Substring( 0, 2 ) == "rS")
            {

                command.Add( 0x06 );    // ENQ
                command.AddRange( SoftBasic.BuildAsciiBytesFrom( byte.Parse( station ) ) );
                command.Add( 0x72 );    // command r
                command.Add( 0x53 );    // command type: SB
                command.Add( 0x42 );
                command.AddRange( Encoding.ASCII.GetBytes( "01" ) );
                StartAddress = DeviceAddress.Remove( 0, 3 );
                bool[] data;
                string txtValue;
                switch (DeviceAddress.Substring( 1, 2 ))
                {
                    case "MB":
                    case "PB":
                        var dbint = Convert.ToInt32( size, 16 ) * 8;
                        int startIndex = int.Parse( StartAddress );
                        switch (DeviceAddress[1])
                        {
                            case 'P':
                                data = pBuffer.GetBool( startIndex, dbint );
                                break;
                            case 'Q':
                                data = qBuffer.GetBool( startIndex, dbint );
                                break;
                            case 'M':
                                data = mBuffer.GetBool( startIndex, dbint );
                                break;
                            case 'D':
                                data = dBuffer.GetBool( startIndex, dbint );
                                break;
                            default: throw new Exception( StringResources.Language.NotSupportedDataType );
                        }
                        var data3 = SoftBasic.BoolArrayToByte( data );
                        txtValue = GetValStr( data3, 0, Convert.ToInt32( size, 16 ) );
                        command.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)data3.Length ) );
                        command.AddRange( SoftBasic.BytesToAsciiBytes( data3 ) );
                        command.Add( 0x03 );    // ETX
                        int sum1 = 0;
                        for (int i = 0; i < command.Count; i++)
                        {
                            sum1 += command[i];
                        }
                        command.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)sum1 ) );
                        break;
                    case "DB":
                    case "TB":
                        var RequestCount = Convert.ToInt32( size, 16 );
                        byte[] dataW;
                        var startIndexW = int.Parse( StartAddress );
                        switch (DeviceAddress[1])
                        {
                            case 'P':
                                dataW = pBuffer.GetBytes( startIndexW, (ushort)RequestCount );
                                break;
                            case 'Q':
                                dataW = qBuffer.GetBytes( startIndexW, (ushort)RequestCount );
                                break;
                            case 'M':
                                dataW = mBuffer.GetBytes( startIndexW, (ushort)RequestCount );
                                break;
                            case 'D':
                                dataW = dBuffer.GetBytes( startIndexW, (ushort)RequestCount );
                                break;
                            default: throw new Exception( StringResources.Language.NotSupportedDataType );
                        }
                        txtValue = GetValStr( dataW, 0, Convert.ToInt32( size, 16 ) );
                        command.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)dataW.Length ) );
                        command.AddRange( SoftBasic.BytesToAsciiBytes( dataW ) );
                        command.Add( 0x03 );    // ETX
                        int sum = 0;
                        for (int i = 0; i < command.Count; i++)
                        {
                            sum += command[i];
                        }
                        command.AddRange( SoftBasic.BuildAsciiBytesFrom( (byte)sum ) );
                        break;
                }
                return command.ToArray( );
            }
            else
            {
                StartAddress = DeviceAddress.Remove( 0, 3 );
                command.Add( 0x06 );    // ENQ
                command.AddRange( SoftBasic.BuildAsciiBytesFrom( byte.Parse( station ) ) );
                command.Add( 0x77 );    // command w
                command.Add( 0x53 );    // command type: SB
                command.Add( 0x42 );
                command.Add( 0x03 );    // EOT
                string Value;
                if (Read.Substring( 0, 3 ) == "WSS")
                {
                    //nameLength = packet.Substring(8, 1);
                    //DeviceAddress = packet.Substring(9, Convert.ToInt16(nameLength));
                    //AddressLength = packet.Substring(9 + Convert.ToInt16(nameLength), 1);
                    nameLength = Encoding.ASCII.GetString( packet, 8, 2 );
                    DeviceAddress = Encoding.ASCII.GetString( packet, 10, int.Parse( nameLength ) );
                    Value = Encoding.ASCII.GetString( packet, 10 + int.Parse( nameLength ), 2 );
                }
                else
                {
                    //Value = Encoding.ASCII.GetString(packet, 10 + int.Parse(nameLength), int.Parse(size));
                    Value = Encoding.ASCII.GetString( packet, 8 + int.Parse( nameLength ) + int.Parse( size ), int.Parse( size ) * 2 );
                    var wdArys = HexToBytes( Value );
                }


                var startIndex = CheckAddress( StartAddress );
                switch (DeviceAddress.Substring( 1, 2 ))
                {
                    case "MX": // Bit X
                        Value = Encoding.ASCII.GetString( packet, 8 + int.Parse( nameLength ) + int.Parse( size ), int.Parse( size ) );
                        switch (DeviceAddress[1])
                        {
                            //case 'M': inputBuffer.SetBool(value, startIndex); break;
                            //case 'M': outputBuffer.SetBool(value, startIndex); break;
                            case 'M': mBuffer.SetBool( Value == "01" ? true : false, startIndex ); break;
                            case 'D': dBuffer.SetBool( Value == "01" ? true : false, startIndex ); break;
                            default: throw new Exception( StringResources.Language.NotSupportedDataType );
                        }
                        return command.ToArray( );
                    case "DW": //Word
                        Value = Encoding.ASCII.GetString( packet, 8 + int.Parse( nameLength ) + int.Parse( size ), int.Parse( size ) * 2 );
                        var wdArys = HexToBytes( Value );
                        switch (DeviceAddress[1])
                        {
                            case 'C': pBuffer.SetBytes( wdArys, startIndex ); break;
                            case 'T': qBuffer.SetBytes( wdArys, startIndex ); break;
                            case 'M': mBuffer.SetBytes( wdArys, startIndex ); break;
                            case 'D': dBuffer.SetBytes( wdArys, startIndex ); break;
                            default: throw new Exception( StringResources.Language.NotSupportedDataType );
                        }
                        return command.ToArray( );
                    case "DD": //DWord


                        break;
                    case "DL": //LWord

                        break;

                    default:

                        return null;
                }
            }
            return command.ToArray( );
        }

#endif

        #endregion

        #region Object Override

        /// <summary>
        /// 返回表示当前对象的字符串
        /// </summary>
        /// <returns>字符串信息</returns>
        public override string ToString( )
        {
            return $"LSisServer[{Port}]";
        }

        #endregion
    }
}
