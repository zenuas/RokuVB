Imports System.Reflection
Imports System.Reflection.Emit
Imports Roku.Manager


Namespace Architecture.CIL

    <ArchitectureName("CIL")>
    Public Class CommonIL
        Implements IArchitecture

        Public Overridable Property Root As RkNamespace
        Public Overridable Property EntryPoint As String = "Global"
        Public Overridable Property Subsystem As PEFileKinds = PEFileKinds.ConsoleApplication
        Public Overridable Property Assembly As AssemblyBuilder
        Public Overridable Property [Module] As ModuleBuilder

        Public Overridable Sub Assemble(ns As RkNamespace) Implements IArchitecture.Assemble

            Me.Root = ns

            Dim name As New AssemblyName(Me.EntryPoint)
            Me.Assembly = System.AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save)
            Me.Module = Me.Assembly.DefineDynamicModule(Me.EntryPoint, System.IO.Path.GetRandomFileName, False)
        End Sub

        Public Overridable Sub Optimize() Implements IArchitecture.Optimize

            'Throw New NotImplementedException()
        End Sub

        Public Overridable Sub Emit(path As String) Implements IArchitecture.Emit

            If Me.Subsystem <> PEFileKinds.Dll Then

                ' global sub main() {EntryPoint.new();}
                Dim method = Me.Module.DefineGlobalMethod("__EntryPoint", MethodAttributes.Static Or MethodAttributes.Family, GetType(System.Void), System.Type.EmptyTypes)

                Dim il = method.GetILGenerator
                'il.Emit(OpCodes.Newobj, Me.Module.GetType(Me.EntryPoint).GetConstructor(System.Type.EmptyTypes))
                'il.Emit(OpCodes.Pop)
                il.Emit(OpCodes.Ret)

                Me.Assembly.SetEntryPoint(method, Me.Subsystem)
            End If

            Me.Module.CreateGlobalFunctions()
            Dim temp = System.IO.Path.GetFileName(Me.Module.FullyQualifiedName)
            Try
                Me.Assembly.Save(temp)
                System.IO.File.Copy(temp, path, True)

            Finally
                System.IO.File.Delete(temp)

            End Try
        End Sub
    End Class

End Namespace
