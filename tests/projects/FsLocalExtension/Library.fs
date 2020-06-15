namespace FsLocalExtension

open AutoStep.Extensions
open Microsoft.Extensions.Configuration
open Autofac

type MyExtension() = 
  interface IExtensionEntryPoint with
    member this.AttachToProject(projectConfig: IConfiguration, project: AutoStep.Projects.Project): unit = 
      raise (System.NotImplementedException())
    member this.Dispose(): unit = 
      raise (System.NotImplementedException())
    member this.ExtendExecution(projectConfig: IConfiguration, testRun: AutoStep.Execution.TestRun): unit = 
      raise (System.NotImplementedException())
