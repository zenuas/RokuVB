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
            Me.DeclareStatements(functions, structs)

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

        Public Overridable Sub DeclareStatements(functions As Dictionary(Of RkFunction, MethodBuilder), structs As Dictionary(Of RkStruct, TypeBuilder))

            For Each f In functions

                Dim il = f.Value.GetILGenerator
                Dim locals As New Dictionary(Of String, Integer)
                Dim max_local = 0
                Util.Functions.Do(f.Key.Arguments, Sub(v, i) locals(v.Name) = -i - 1)

                Dim get_local =
                    Function(v As RkValue)

                        If locals.ContainsKey(v.Name) Then Return locals(v.Name)
                        il.DeclareLocal(Me.RkStructToCILType(v.Type, structs))
                        locals(v.Name) = max_local
                        max_local += 1
                        Return max_local - 1
                    End Function

                Dim gen_il_load =
                    Sub(v As RkValue)

                        If TypeOf v Is RkNumeric32 Then

                            Dim num = CType(v, RkNumeric32)
                            Select Case num.Numeric

                                'Case -1 : il.Emit(OpCodes.Ldc_I4_M1)
                                Case 0 : il.Emit(OpCodes.Ldc_I4_0)
                                Case 1 : il.Emit(OpCodes.Ldc_I4_1)
                                Case 2 : il.Emit(OpCodes.Ldc_I4_2)
                                Case 3 : il.Emit(OpCodes.Ldc_I4_3)
                                Case 4 : il.Emit(OpCodes.Ldc_I4_4)
                                Case 5 : il.Emit(OpCodes.Ldc_I4_5)
                                Case 6 : il.Emit(OpCodes.Ldc_I4_6)
                                Case 7 : il.Emit(OpCodes.Ldc_I4_7)
                                Case 8 : il.Emit(OpCodes.Ldc_I4_8)

                                Case Else
                                    il.Emit(OpCodes.Ldc_I4, num.Numeric)

                            End Select

                        ElseIf TypeOf v Is RkString Then

                            Dim str = CType(v, RkString)
                            il.Emit(OpCodes.Ldstr, str.String)
                        Else
                        End If
                    End Sub

                Dim gen_il_store =
                    Sub(v As RkValue)

                        Dim index = get_local(v)
                        Select Case index

                            Case 0 : il.Emit(OpCodes.Stloc_0)
                            Case 1 : il.Emit(OpCodes.Stloc_1)
                            Case 2 : il.Emit(OpCodes.Stloc_2)
                            Case 3 : il.Emit(OpCodes.Stloc_3)
                            Case Else

                                If index >= 0 Then

                                    il.Emit(OpCodes.Stloc, index)
                                Else

                                    il.Emit(OpCodes.Starg, -(index + 1))
                                End If
                        End Select
                    End Sub

                Dim gen_il_3op =
                    Sub(ope As OpCode, code As RkCode)

                        gen_il_load(code.Left)
                        gen_il_load(code.Right)
                        il.Emit(ope)
                        If code.Return IsNot Nothing Then gen_il_store(code.Return)
                    End Sub

                For Each stmt In f.Key.Body

                    Select Case stmt.Operator
                        Case RkOperator.Plus : gen_il_3op(OpCodes.Add, CType(stmt, RkCode))
                        Case RkOperator.Minus : gen_il_3op(OpCodes.Sub, CType(stmt, RkCode))
                        Case RkOperator.Mul : gen_il_3op(OpCodes.Mul, CType(stmt, RkCode))
                        Case RkOperator.Div : gen_il_3op(OpCodes.Div, CType(stmt, RkCode))

                    End Select
                Next
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
