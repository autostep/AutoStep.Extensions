namespace FsLocalExtension

open AutoStep.Extensions
open Microsoft.Extensions.Configuration

type MyExtension() = 
  interface IExtensionEntryPoint with
    member this.AttachToProject(projectConfig: IConfiguration, project: AutoStep.Projects.Project): unit = 
      raise (System.NotImplementedException())
    member this.ConfigureExecutionServices(runConfiguration: IConfiguration, servicesBuilder: AutoStep.Execution.Dependency.IServicesBuilder): unit = 
      raise (System.NotImplementedException())
    member this.Dispose(): unit = 
      raise (System.NotImplementedException())
    member this.ExtendExecution(projectConfig: IConfiguration, testRun: AutoStep.Execution.TestRun): unit = 
      raise (System.NotImplementedException())
