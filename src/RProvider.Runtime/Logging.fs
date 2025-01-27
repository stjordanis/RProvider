﻿/// [omit]
module RProvider.Internal.Logging

open System
open System.IO
open System.Diagnostics
open RProvider

let private logEnvVar = "RPROVIDER_LOG"

/// The logging is enabled by setting the RPROVIDER_LOG environment variable
/// Alternatively, just change this constant to 'true' and logs will be 
/// saved in the default location (see below)
let private loggingEnabled = 
    Environment.GetEnvironmentVariable(logEnvVar) <> null

/// Log file - if the RPROVIDER_LOG variable is not set, the default on  
/// Windows is "C:\Users\<user>\AppData\Roaming\RLogs\log.txt" and on Mac 
/// this is in "/User/<user>/.config/RLogs/log.txt")
let private logFile = 
  try
    let var = Environment.GetEnvironmentVariable(logEnvVar)
    if var <> null then var else
      let appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
      if not (Directory.Exists(appData + "/RLogs")) then Directory.CreateDirectory(appData + "/RLogs") |> ignore
      appData + "/RLogs/log.txt"
  with _ -> (* Silently ignoring logging errors *) null

/// Append string to a log file
let private writeString str =
  try
    // This serializes all writes to the log file (from multiple processes)
    use fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Write, 4096, FileOptions.None)
    use writer = new StreamWriter(fs)
    writer.AutoFlush <- true

    let pid = Process.GetCurrentProcess().Id
    let tid = System.Threading.Thread.CurrentThread.ManagedThreadId
    let apid = System.AppDomain.CurrentDomain.Id
    writer.WriteLine(sprintf "[%s] [Pid:%d, Tid:%d, Apid:%d] %s" (DateTime.Now.ToString("G")) pid tid apid str)
  with _ -> (*silently ignoring logging errors*) () 

/// Log formatted string to a log file
let logf fmt = 
    let f = if loggingEnabled then writeString else ignore
    Printf.kprintf f fmt

/// Run the specified function and log potential expceptions, as well
/// as the output written to console (unless characterDevice.IsCapturing)
let internal logWithOutput (characterDevice:CharacterDeviceInterceptor) f =
  if loggingEnabled then 
    try 
      // If the device is capturing stuff for someone else, then
      // we do not want to touch it (it is a minor limitation only)
      let capturing = characterDevice.IsCapturing
      try
        if not capturing then characterDevice.BeginCapture()
        f()
      finally
        let out = 
          if not capturing then characterDevice.EndCapture()
          else "(could not be captured)"
        logf $"Output: %s{out}"
    with e -> 
      logf $"Operation failed:\r\n  {e}"
      reraise()
  else f ()  