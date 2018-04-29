Imports System
Imports System.Collections.Generic
Imports System.Reflection
Imports Roku.Parser
Imports Roku.Node
Imports Roku.Util.Extensions


<Assembly: AssemblyVersion("0.0.*")>
Public Class Main

    Public Shared Function Main(args() As String) As Integer

#If DEBUG Then
        Run(args)
        Return 0
#Else
        Try
            Run(args)

            Return 0

        Catch ex As Exception

            Do While ex IsNot Nothing

                Console.Error.WriteLine(ex.Message)
                ex = ex.InnerException
            Loop
            Return 1

        End Try
#End If
    End Function

    Public Shared Sub Run(args() As String)

        Dim opt As New Command.Option
        Dim xs = Command.Parser.Parse(opt, args)
        Dim loader As New Loader With {.CurrentDirectory = System.IO.Directory.GetCurrentDirectory}
        loader.LoadAssembly("mscorlib")
        opt.Libraries.Each(Sub(x) loader.LoadAssembly(x))

        If xs.Length = 0 Then

            opt.EntryPoiny = ""
            loader.LoadModule("", System.Console.In)
        Else

            If opt.EntryPoiny Is Nothing Then opt.EntryPoiny = xs(0)
            For Each arg In xs

                loader.LoadModule(arg)
            Next
        End If
        Compile(loader, opt)
    End Sub

    Public Shared Sub Compile(loader As Loader, opt As Command.Option)

        If opt.NodeDump IsNot Nothing Then NodeDumpGraph(opt.NodeDump, loader.Root)

        Dim root As New Manager.SystemLibrary With {.Name = "Global"}
        Using x As New IO.StreamReader(Assembly.GetExecutingAssembly.GetManifestResourceStream("Roku.sys.rk"))

            loader.LoadModule("Sys", x)
        End Using

        For Each ns In loader.Root.Namespaces

            Dim pgm = ns.Value
            Dim current = root.CreateNamespace(ns.Key)
            current.AddLoadPath(root)
            Compiler.NameResolver.ResolveName(pgm)
            Compiler.Normalize.Normalization(pgm)
            Compiler.Closure.Capture(pgm)
            Compiler.Typing.Prototype(pgm, root, current)
        Next

        For Each asm In loader.Assemblies

            root.LoadAssembly(asm)
        Next

        For Each ns In loader.Root.Namespaces

            Dim pgm = ns.Value
            Dim current = root.GetNamespace(ns.Key)

            For Each use In pgm.Uses

                current.AddLoadPath(root.TryGetNamespace(use.GetNamespaceHierarchy))
            Next
            Compiler.Typing.TypeStatic(pgm, root, current)
        Next

        For Each ns In loader.Root.Namespaces

            Compiler.Typing.TypeInference(ns.Value, root, root.GetNamespace(ns.Key))
            Compiler.Typing.AnonymouseTypeAllocation(ns.Value, root, root.GetNamespace(ns.Key))
        Next
        If opt.TypeResult IsNot Nothing Then TypeResult(opt.TypeResult, loader.Root)

        For Each ns In loader.Root.Namespaces

            Compiler.Translater.ClosureTranslate(ns.Value, root, root.GetNamespace(ns.Key))
            Compiler.Translater.Translate(ns.Value, root, root.GetNamespace(ns.Key))
        Next

        Dim arch As New Architecture.CommonIL
        arch.Assemble(root, root.Namespaces(loader.FileNameToNamespace(opt.EntryPoiny)), opt.Output, Emit.PEFileKinds.ConsoleApplication)
    End Sub

    Public Shared Sub TypeResult(out As IO.TextWriter, node As INode)

        Util.Traverse.NodesOnce(
            node,
            0,
            Sub(parent, ref, child, user, isfirst, next_)

                Dim name = child.GetType.Name
                If child.LineNumber.HasValue Then name += $"( {child.LineNumber}, {child.LineColumn} )"
                Select Case True
                    Case TypeOf child Is VariableNode : Dim v = CType(child, VariableNode) : name += $" {v.Name} : {v.Type?.ToString}"
                    Case TypeOf child Is TypeNode : Dim v = CType(child, TypeNode) : name += $" {v.Name} : {v.Type?.ToString}"
                    Case TypeOf child Is NumericNode : Dim v = CType(child, NumericNode) : name += $" {v.Numeric} : {v.Type?.ToString}"
                    Case TypeOf child Is StringNode : Dim v = CType(child, StringNode) : name += $" {v.String} : {v.Type?.ToString}"
                    Case TypeOf child Is FunctionNode : Dim v = CType(child, FunctionNode) : name += $" {v.Name} : {v.Type?.ToString}"
                    Case TypeOf child Is ExpressionNode : Dim v = CType(child, ExpressionNode) : name += $" {v.Operator} : {v.Type?.ToString}"
                    Case TypeOf child Is DeclareNode : Dim v = CType(child, DeclareNode) : name += $" {v.Name.Name} : {v.Type?.ToString}"
                    Case TypeOf child Is LetNode : Dim v = CType(child, LetNode) : name += $" {v.Var.Name} : {v.Type?.ToString}"
                    Case TypeOf child Is StructNode : Dim v = CType(child, StructNode) : name += $" {v.Name} : {v.Type?.ToString}"
                    Case TypeOf child Is FunctionCallNode : Dim v = CType(child, FunctionCallNode) : name += $" {v.Function?.ToString} : {v.Type?.ToString}"
                End Select

                out.WriteLine($"{String.Join("", "  ".Pattern(user))}{name}")

                next_(child, user + 1)
            End Sub)
    End Sub

    Public Shared Sub NodeDumpGraph(out As IO.TextWriter, node As INode)

        out.WriteLine("
digraph roku {
graph [
compound = true
];
node [
shape = record,
align = left,
];")

        Dim used As New Dictionary(Of INode, Boolean)

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Sub(parent, ref, child, user, isfirst, next_)

                If TypeOf parent Is RootNode Then

                    If ref.Equals("`Sys") Then Return
                    out.WriteLine($"subgraph cluster_root_{child.GetHashCode} {{")
                    out.WriteLine("  style=solid;")
                    out.WriteLine("  color=black;")
                    out.WriteLine($"  label = ""{ref}"";")
                End If

                If TypeOf child Is BlockNode Then

                    Dim block = CType(child, BlockNode)
                    out.WriteLine($"subgraph cluster_block_stmt_{block.GetHashCode} {{")
                    out.WriteLine("  style=solid;")
                    out.WriteLine("  color=black;")
                    out.WriteLine("  node [style=filled];")

                    Dim name = "block"
                    Dim args = ""
                    If parent Is Nothing Then

                        name = "entrypoint"

                    ElseIf TypeOf parent Is FunctionNode AndAlso ref.Equals("Body") Then

                        Dim func = CType(parent, FunctionNode)
                        name = func.Name
                        args = "\l" + String.Join("\l", func.Arguments.Map(Function(arg) $"{arg.Name.Name} : {arg.Type.Name}"))
                    End If

                    out.WriteLine($"  label = ""{name} ({block.LineNumber}){args}"";")
                    out.WriteLine("  " + String.Join(" -> ", block.Statements.Map(Function(x) x.GetHashCode.ToString)))
                    out.WriteLine("}")

                ElseIf parent IsNot Nothing AndAlso
                    TypeOf parent IsNot BlockNode AndAlso
                    TypeOf child IsNot DeclareNode AndAlso
                    TypeOf parent IsNot DeclareNode Then

                    out.WriteLine($"{parent.GetHashCode} -> {child.GetHashCode} [label = ""{ref}""];")
                    used(parent) = True
                    used(child) = True
                End If

                next_(child, user)

                If TypeOf parent Is RootNode Then

                    out.WriteLine("}")
                End If
            End Sub)

        Util.Traverse.NodesOnce(Of Object)(
            node,
            Nothing,
            Sub(parent, ref, child, user, isfirst, next_)

                If isfirst AndAlso used.ContainsKey(child) Then

                    Dim name = ""
                    If child.LineNumber.HasValue Then name = $"\l( {child.LineNumber}, {child.LineColumn} )"
                    Select Case True
                        Case TypeOf child Is VariableNode : Dim v = CType(child, VariableNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is TypeNode : Dim v = CType(child, TypeNode) : name += $"\l{v.Name}"
                        Case TypeOf child Is NumericNode : Dim v = CType(child, NumericNode) : name += $"\l{v.Numeric}"
                        Case TypeOf child Is StringNode : Dim v = CType(child, StringNode) : name += $"\l{v.String}"
                        Case TypeOf child Is FunctionNode : Dim v = CType(child, FunctionNode) : name += $"\l{v.Name}"
                        Case TypeOf child Is ExpressionNode : Dim v = CType(child, ExpressionNode) : name += $"\l{v.Operator}\l`{v.Type?.Name}`"
                        Case TypeOf child Is DeclareNode : Dim v = CType(child, DeclareNode) : name += $"\l{v.Name.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is LetNode : Dim v = CType(child, LetNode) : name += $"\l{v.Var.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is StructNode : Dim v = CType(child, StructNode) : name += $"\l{v.Name}\l`{v.Type?.Name}`"
                        Case TypeOf child Is FunctionCallNode : Dim v = CType(child, FunctionCallNode) : name += $"\lsub {v.Function?.ToString}"
                    End Select

                    out.WriteLine($"{child.GetHashCode} [label = ""{child.GetType.Name}{name}""]")
                End If

                next_(child, user)
            End Sub)

        out.WriteLine("}")
    End Sub

End Class
