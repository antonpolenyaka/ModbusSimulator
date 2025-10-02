using ModbusSimulator.Domain.Entities;
using System.Net;
using System.Net.Sockets;

namespace ModbusSimulator.Infrastructure.ModbusTcp
{
    /// <summary>
    /// Creates a Modbus TCP server bound to the given IP and port.
    /// </summary>
    public class ModbusServer(SlaveStateService stateService, string ip = "0.0.0.0", int port = 502)
    {
        private readonly IPAddress _ipAddress = IPAddress.Parse(ip);
        private readonly int _port = port;
        private TcpListener? _listener;
        private bool _isRunning;
        private readonly List<ModbusSlave> _slaves = [];
        private readonly SlaveStateService _stateService = stateService;

        /// <summary>
        /// Add a slave to the server
        /// </summary>
        public void AddSlave(ModbusSlave slave)
        {
            ArgumentNullException.ThrowIfNull(slave);
            _slaves.Add(slave);
        }

        /// <summary>
        /// Start listening for Modbus TCP connections
        /// </summary>
        public void Start()
        {
            _listener = new TcpListener(_ipAddress, _port);
            _listener.Start();
            _isRunning = true;

            Thread listenerThread = new(ListenForClients)
            {
                IsBackground = true
            };
            listenerThread.Start();

            Console.WriteLine($"Modbus TCP Server listening on {_ipAddress}:{_port}");
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }

        /// <summary>
        /// Main loop to accept incoming client connections
        /// </summary>
        private void ListenForClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener!.AcceptTcpClient();
                    Thread clientThread = new(() => HandleClient(client))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    // Listener stopped
                    break;
                }
            }
        }

        /// <summary>
        /// Handle an individual client
        /// </summary>
        private void HandleClient(TcpClient client)
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[256];

                while (_isRunning && client.Connected)
                {
                    int bytesRead = 0;
                    try
                    {
                        if (!stream.DataAvailable) { Thread.Sleep(10); continue; }

                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        // Handle Modbus request
                        byte slaveId = buffer[6];
                        // Now we fetch the slave from the shared state service
                        ModbusSlave? slave = _stateService.Slaves.FirstOrDefault(s => s.SlaveId == slaveId);
                        if (slave == null)
                        {
                            // Build exception response: functionCode | 0x80, exceptionCode 0x0B
                            byte transactionIdHigh = buffer[0];
                            byte transactionIdLow = buffer[1];
                            byte[] responseException = ModbusRequestHandler.ExceptionResponse(
                                (ushort)((transactionIdHigh << 8) + transactionIdLow),
                                slaveId,
                                buffer[7], // function code
                                0x0B      // dummy code for “Slave Device Failure” or Illegal Slave
                            );
                            stream.Write(responseException, 0, responseException.Length);
                            continue;
                        }

                        byte[] response = ModbusRequestHandler.ProcessRequest(buffer, bytesRead, slave);

                        if (response != null && response.Length > 0)
                            stream.Write(response, 0, response.Length);
                    }
                    catch (Exception ex)
                    {
                        // If the exception occurs, we return a generic Modbus error
                        if (bytesRead >= 8) // Make sure we have MBAP
                        {
                            ushort transactionId = (ushort)((buffer[0] << 8) + buffer[1]);
                            byte unitId = buffer[6];
                            byte functionCode = buffer[7];
                            byte[] errorResponse = ModbusRequestHandler.ExceptionResponse(transactionId, unitId, functionCode, 0x04); // 0x04 = Server Device Failure
                            try
                            {
                                stream.Write(errorResponse, 0, errorResponse.Length);
                            }
                            catch {/* ignore failure when writing response */ }
                        }

                        Console.WriteLine($"Client error: {ex.Message}");
                        break;
                    }
                }
            }
            client.Close();
        }
    }
}
