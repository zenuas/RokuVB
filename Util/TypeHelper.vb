Imports System
Imports System.Reflection
Imports Roku.Util.Extensions


Namespace Util

    Public Class TypeHelper

        Public Shared Function IsInterface(p As Type, interf As Type) As Boolean

            Return p Is interf OrElse p.GetTypeInfo.ImplementedInterfaces.FindFirstOrNull(Function(x) x Is interf) IsNot Nothing
        End Function

        Public Shared Function IsGeneric(p As Type, generic As Type) As Boolean

            Return p.IsGenericType AndAlso p.GetTypeInfo.GetGenericTypeDefinition Is generic
        End Function

        Public Overloads Shared Function MemberwiseClone(p As Object) As Object

            Return p.GetType.InvokeMember("MemberwiseClone", BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.InvokeMethod, Nothing, p, Nothing)
        End Function

    End Class

End Namespace
