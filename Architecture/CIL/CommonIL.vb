Imports System
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Reflection.Emit
Imports Roku.Manager
Imports Roku.Util.ArrayExtension


Namespace Architecture.CIL

    <ArchitectureName("CIL")>
    Public Class CommonIL
        Implements IArchitecture

#Region "export libs"

        Public Class Export

            Public Overridable Property Name As String
            Public Overridable Property Assembly As String
            Public Overridable Property [Class] As String
            Public Overridable Property Method As String
            Public Overridable Property Arguments As Type()
        End Class

        Public Overridable ReadOnly Property Exports As Export() = New Export() {
                New Export With {.Name = "print", .Assembly = "mscorlib", .Class = "System.Console", .Method = "WriteLine", .Arguments = New Type() {GetType(String)}},
                New Export With {.Name = "print", .Assembly = "mscorlib", .Class = "System.Console", .Method = "WriteLine", .Arguments = New Type() {GetType(Integer)}}
            }

#End Region

        Public Overridable Property Root As RkNamespace
        Public Overridable Property EntryPoint As String = "Global"
        Public Overridable Property Subsystem As PEFileKinds = PEFileKinds.ConsoleApplication
        Public Overridable Property Assembly As AssemblyBuilder
        Public Overridable Property [Module] As ModuleBuilder
        Public Overridable Property IsDebug As Boolean = True

        Public Overridable Sub Assemble(ns As RkNamespace) Implements IArchitecture.Assemble

            Me.Root = ns

            Dim name As New AssemblyName(Me.EntryPoint)
            Me.Assembly = System.AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save)
            Me.Module = Me.Assembly.DefineDynamicModule(Me.EntryPoint, System.IO.Path.GetRandomFileName, Me.IsDebug)

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
            Dim pdb = System.IO.Path.GetFileNameWithoutExtension(temp) + ".pdb"
            Try
                Me.Assembly.Save(temp)
                System.IO.File.Copy(temp, path, True)
                System.IO.File.Copy(temp, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".pdb"), True)

            Finally
                System.IO.File.Delete(temp)
                System.IO.File.Delete(pdb)

            End Try
        End Sub

        Public Overridable Function DeclareStructs(ns As RkNamespace) As Dictionary(Of RkStruct, TypeBuilder)

            Dim map As New Dictionary(Of RkStruct, TypeBuilder)
            Return map
        End Function

        Public Overridable Function DeclareMethods(ns As RkNamespace, structs As Dictionary(Of RkStruct, TypeBuilder)) As Dictionary(Of RkFunction, MethodInfo)

            Dim map As New Dictionary(Of RkFunction, MethodInfo)
            For Each fs In ns.Functions

                For Each f In fs.Value.Where(Function(x) Not x.HasGeneric AndAlso TypeOf x IsNot RkNativeFunction AndAlso Not x.Name.Equals("return"))

                    Dim args = Me.RkStructToCILType(f.Arguments, structs)
                    Dim export = Me.Exports.Where(Function(x) f.Name.Equals(x.Name) AndAlso args.And(Function(arg, i) arg Is x.Arguments(i)))
                    If export.IsNull Then

                        Debug.Assert(f.Body.Count > 0, $"{f} statement is nothing")
                        map(f) = Me.Module.DefineGlobalMethod(f.CreateManglingName, MethodAttributes.Static Or MethodAttributes.Public, Me.RkStructToCILType(f.Return, structs), args)
                    Else

                        Dim e = export.Car
                        map(f) = System.Reflection.Assembly.Load(e.Assembly).GetType(e.Class).GetMethod(e.Method, e.Arguments)
                    End If
                Next
            Next

            Return map
        End Function

        Public Overridable Sub DeclareStatements(functions As Dictionary(Of RkFunction, MethodInfo), structs As Dictionary(Of RkStruct, TypeBuilder))

            For Each f In functions.Where(Function(x) TypeOf x.Value Is MethodBuilder)

                Dim il = CType(f.Value, MethodBuilder).GetILGenerator
                Dim locals = f.Key.Arguments.Map(Function(x) x.Name).ToHash_ValueDerivation(Function(x, i) -i - 1)
                Dim max_local = 0

                Dim get_local =
                    Function(v As RkValue)

                        Dim name = v.Name
                        If locals.ContainsKey(name) Then Return locals(name)
                        il.DeclareLocal(Me.RkStructToCILType(v.Type, structs)).SetLocalSymInfo(name)
                        locals(name) = max_local
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

                        ElseIf TypeOf v Is RkValue Then

                            Dim index = get_local(v)
                            Select Case index

                                Case 0 : il.Emit(OpCodes.Ldloc_0)
                                Case 1 : il.Emit(OpCodes.Ldloc_1)
                                Case 2 : il.Emit(OpCodes.Ldloc_2)
                                Case 3 : il.Emit(OpCodes.Ldloc_3)

                                Case -1 : il.Emit(OpCodes.Ldarg_0)
                                Case -2 : il.Emit(OpCodes.Ldarg_1)
                                Case -3 : il.Emit(OpCodes.Ldarg_2)
                                Case -4 : il.Emit(OpCodes.Ldarg_3)

                                Case Else

                                    If index >= 0 Then

                                        il.Emit(OpCodes.Ldloc, index)
                                    Else

                                        il.Emit(OpCodes.Ldarg, -(index + 1))
                                    End If
                            End Select
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

                Dim found_ret = False
                For Each stmt In f.Key.Body

                    Select Case stmt.Operator
                        Case RkOperator.Plus : gen_il_3op(OpCodes.Add, CType(stmt, RkCode))
                        Case RkOperator.Minus : gen_il_3op(OpCodes.Sub, CType(stmt, RkCode))
                        Case RkOperator.Mul : gen_il_3op(OpCodes.Mul, CType(stmt, RkCode))
                        Case RkOperator.Div : gen_il_3op(OpCodes.Div, CType(stmt, RkCode))

                        Case RkOperator.Call
                            Dim cc = CType(stmt, RkCall)
                            cc.Arguments.Do(Sub(arg) gen_il_load(arg))
                            il.Emit(OpCodes.Call, functions(cc.Function))
                            If cc.Return IsNot Nothing Then gen_il_store(cc.Return)

                        Case RkOperator.Return
                            If TypeOf stmt Is RkCode Then gen_il_load(CType(stmt, RkCode).Left)
                            il.Emit(OpCodes.Ret)
                            found_ret = True

                    End Select
                Next
                If Not found_ret Then il.Emit(OpCodes.Ret)
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

            Return r.Map(Function(x) Me.RkStructToCILType(x.Value, structs)).ToArray
        End Function
    End Class

End Namespace
