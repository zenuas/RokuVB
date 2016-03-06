Imports System
Imports System.Collections.Generic
Imports System.Reflection


Namespace Command

    Public Class Parser

        Public Shared Function GetCommand(receiver As Object) As Dictionary(Of String, MethodInfo)

            Dim opt_map As New Dictionary(Of String, MethodInfo)

            For Each method In receiver.GetType.GetMethods(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)

                Dim x = GetCommandLine(method)
                If x IsNot Nothing Then

                    If Not String.IsNullOrEmpty(x.ShortOption) Then opt_map.Add(x.ShortOption, method)
                    If Not String.IsNullOrEmpty(x.LongOption) Then

                        opt_map.Add(x.LongOption, method)
                    Else
                        opt_map.Add(method.Name.ToLower, method)
                    End If
                End If
            Next

            For Each prop In receiver.GetType.GetProperties(BindingFlags.FlattenHierarchy Or BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.SetProperty)

                Dim x = GetCommandLine(prop)
                Dim method = prop.GetSetMethod(True)
                If x IsNot Nothing Then

                    If Not String.IsNullOrEmpty(x.ShortOption) Then opt_map.Add(x.ShortOption, method)
                    If Not String.IsNullOrEmpty(x.LongOption) Then

                        opt_map.Add(x.LongOption, method)
                    Else
                        opt_map.Add(method.Name.ToLower, method)
                    End If
                End If
            Next

            Return opt_map
        End Function

        Public Shared Function GetCommandLine(member As MemberInfo) As CommandLineAttribute

            Dim xs = member.GetCustomAttributes(GetType(CommandLineAttribute), True)
            If xs.Length = 0 Then Return Nothing
            Return CType(xs(0), CommandLineAttribute)
        End Function

        Public Shared Function FindCommand(opts As Dictionary(Of String, MethodInfo), name As String) As MethodInfo

            name = name.ToLower

            Dim find As String = Nothing
            For Each key In opts.Keys

                If key.StartsWith(name) Then

                    If key.Length = name.Length Then Return opts(name)
                    If find IsNot Nothing Then Throw New Exception($"cannot interpret ``{name}''")
                    find = key
                End If
            Next

            If find IsNot Nothing Then Return opts(find)

            Throw New Exception($"unknown option ``{name}''")
        End Function

        Public Shared Function Parse(receiver As Object, args() As String) As String()

            Dim opts = GetCommand(receiver)
            Dim method As MethodInfo = Nothing
            Dim method_args As New List(Of Object)
            Dim rets As New List(Of String)

            For i = 0 To args.Length - 1

                'If args(i).Length = 0 Then Continue For
                If args(i).Length > 1 AndAlso args(i).Chars(0) = "-"c Then

                    Dim optname As String
                    Dim argument As String = Nothing

                    If args(i).Chars(1) = "-"c Then

                        optname = args(i).Substring(2)
                        If optname.IndexOf("="c) >= 0 Then

                            argument = optname.Substring(optname.IndexOf("="c) + 1)
                            optname = optname.Substring(0, optname.IndexOf("="c))
                        End If
                    Else

                        If args(i).Length > 2 Then argument = args(i).Substring(2)
                        optname = args(i).Chars(1).ToString
                    End If
                    method = FindCommand(opts, optname)

                    If argument IsNot Nothing Then

                        method_args.Add(ToArgType(argument, method.GetParameters(method_args.Count).ParameterType))
                        GoTo ExecuteOption
                    End If

                ElseIf method IsNot Nothing Then

                    method_args.Add(ToArgType(args(i), method.GetParameters(method_args.Count).ParameterType))
                Else

                    rets.Add(args(i))
                End If

                If method IsNot Nothing AndAlso method.GetParameters.Length = method_args.Count Then

ExecuteOption:
                    method.Invoke(receiver, method_args.ToArray)
                    method_args.Clear()
                    method = Nothing
                End If
            Next

            Return rets.ToArray
        End Function

        Public Shared Function ToArgType(o As Object, [type] As System.Type) As Object

            If _
                type Is GetType(System.IO.TextReader) OrElse
                type Is GetType(System.IO.StreamReader) Then

                If o.ToString.Equals("-") Then

                    Return System.Console.In
                Else
                    Return New System.IO.StreamReader(New System.IO.FileStream(o.ToString, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
                End If

            ElseIf _
                type Is GetType(System.IO.TextWriter) OrElse
                type Is GetType(System.IO.StreamWriter) Then

                If o.ToString.Equals("-") Then

                    Return System.Console.Out
                Else
                    Dim out As New System.IO.StreamWriter(New System.IO.FileStream(o.ToString, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Write))
                    out.AutoFlush = True
                    Return out
                End If

            ElseIf type.GetMethod("Parse", New System.Type() {o.GetType}) IsNot Nothing Then

                Return type.InvokeMember("Parse", BindingFlags.InvokeMethod, Nothing, Nothing, New Object() {o})
            Else
                Return o.ToString
            End If
        End Function

    End Class

End Namespace
