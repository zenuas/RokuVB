Imports System
Imports System.Collections.Generic
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

            Dim structs = Me.DeclareStructs(Me.Root)
            Dim functions = Me.DeclareMethods(Me.Root, structs)
            Me.DeclareStatements(functions)

            If Me.Subsystem <> PEFileKinds.Dll Then

                ' global sub main() {EntryPoint.new();}
                Dim method = Me.Module.DefineGlobalMethod("__EntryPoint", MethodAttributes.Static Or MethodAttributes.Family, GetType(System.Void), System.Type.EmptyTypes)

                Dim il = method.GetILGenerator
                Dim ctor = Util.Functions.These(Function() Me.Root.GetFunction(".ctor"))
                If ctor IsNot Nothing Then il.EmitCall(OpCodes.Call, functions(ctor), System.Type.EmptyTypes)
                'il.Emit(OpCodes.Newobj, Me.Module.GetType(Me.EntryPoint).GetConstructor(System.Type.EmptyTypes))
                'il.Emit(OpCodes.Pop)
                il.Emit(OpCodes.Ret)

                Me.Assembly.SetEntryPoint(method, Me.Subsystem)
            End If
            Me.Module.CreateGlobalFunctions()
        End Sub

        Public Overridable Sub Optimize() Implements IArchitecture.Optimize

            'Throw New NotImplementedException()
        End Sub

        Public Overridable Sub Emit(path As String) Implements IArchitecture.Emit

            Dim temp = System.IO.Path.GetFileName(Me.Module.FullyQualifiedName)
            Try
                Me.Assembly.Save(temp)
                System.IO.File.Copy(temp, path, True)

            Finally
                System.IO.File.Delete(temp)

            End Try
        End Sub

        Public Overridable Function DeclareStructs(ns As RkNamespace) As Dictionary(Of RkStruct, TypeBuilder)

            Dim map As New Dictionary(Of RkStruct, TypeBuilder)
            Return map
        End Function

        Public Overridable Function DeclareMethods(ns As RkNamespace, structs As Dictionary(Of RkStruct, TypeBuilder)) As Dictionary(Of RkFunction, MethodBuilder)

            Dim map As New Dictionary(Of RkFunction, MethodBuilder)
            For Each fs In ns.Functions

                For Each f In Util.Functions.Where(fs.Value, Function(x) Not x.HasGeneric)

                    map(f) = Me.Module.DefineGlobalMethod(f.CreateManglingName, MethodAttributes.Static Or MethodAttributes.Public, Me.RkStructToCILType(f.Return, structs), Me.RkStructToCILType(f.Arguments, structs))
                Next
            Next

            Return map
        End Function

        Public Overridable Sub DeclareStatements(functions As Dictionary(Of RkFunction, MethodBuilder))

            For Each f In functions

                Dim il = f.Value.GetILGenerator
                il.Emit(OpCodes.Ret)
            Next
        End Sub

        Public Overridable Function RkStructToCILType(r As IType, structs As Dictionary(Of RkStruct, TypeBuilder)) As System.Type

            If r Is Nothing Then Return GetType(System.Void)
            If TypeOf r IsNot RkStruct Then Throw New ArgumentException("invalid RkStruct", NameOf(r))

            If r.Name.Equals("Int16") Then Return GetType(Int16)
            If r.Name.Equals("Int32") Then Return GetType(Int32)
            If r.Name.Equals("Int64") Then Return GetType(Int64)
            If r.Name.Equals("String") Then Return GetType(String)

            Return structs(CType(r, RkStruct))
        End Function

        Public Overridable Function RkStructToCILType(r As List(Of NamedValue), structs As Dictionary(Of RkStruct, TypeBuilder)) As System.Type()

            Return Util.Functions.List(Util.Functions.Map(r, Function(x) Me.RkStructToCILType(x.Value, structs))).ToArray
        End Function
    End Class

End Namespace
