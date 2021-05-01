module OTLLogger
open Analogy.LogServer.Clients
open Analogy.Interfaces
open Grpc.Core.Logging.
let prod = new AnalogyMessageProducer("http://localhost:6000")
type DebugLogger() =
  let cons = ConsoleLogger()
  interface ILogger with
    override t.Debug s = cons.Debug(s)
//let log sev txt src = prod.Log(txt,src,sev)
//let debugLog = log AnalogyLogLevel.Debug
let logger = new DebugLogger() :> ILogger
let log s = logger.Debug s
