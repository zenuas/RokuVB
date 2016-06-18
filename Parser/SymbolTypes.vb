
Namespace Parser

    Public Enum SymbolTypes As Integer

        ''' <summary>'('</summary>
        __x28 = 0

        ''' <summary>')'</summary>
        __x29

        ''' <summary>','</summary>
        __x2C

        ''' <summary>'.'</summary>
        __x2E

        ''' <summary>':'</summary>
        __x3A

        ''' <summary>'?'</summary>
        __x3F

        ''' <summary>'['</summary>
        __x5B

        ''' <summary>']'</summary>
        __x5D

        ''' <summary>'{'</summary>
        __x7B

        ''' <summary>'}'</summary>
        __x7D

        ''' <summary>ALLOW</summary>
        ALLOW

        ''' <summary>ATVAR</summary>
        ATVAR

        ''' <summary>BEGIN</summary>
        BEGIN

        ''' <summary>ELSE</summary>
        [ELSE]

        ''' <summary>END</summary>
        [END]

        ''' <summary>EOL</summary>
        EOL

        ''' <summary>EQ</summary>
        EQ

        ''' <summary>IF</summary>
        [IF]

        ''' <summary>LET</summary>
        [LET]

        ''' <summary>NULL</summary>
        NULL

        ''' <summary>NUM</summary>
        NUM

        ''' <summary>OPE</summary>
        OPE

        ''' <summary>STR</summary>
        STR

        ''' <summary>STRUCT</summary>
        STRUCT

        ''' <summary>SUB</summary>
        [SUB]

        ''' <summary>USE</summary>
        USE

        ''' <summary>VAR</summary>
        VAR

        ''' <summary>$END</summary>
        _END

        ''' <summary>argn</summary>
        argn

        ''' <summary>args</summary>
        args

        ''' <summary>atvar</summary>
        atvar_1

        ''' <summary>atvarn</summary>
        atvarn

        ''' <summary>begin</summary>
        begin_1

        ''' <summary>block</summary>
        block

        ''' <summary>call</summary>
        [call]

        ''' <summary>decla</summary>
        decla

        ''' <summary>define</summary>
        define

        ''' <summary>elseif</summary>
        [elseif]

        ''' <summary>expr</summary>
        expr

        ''' <summary>extra</summary>
        extra

        ''' <summary>if</summary>
        if_1

        ''' <summary>ifthen</summary>
        ifthen

        ''' <summary>lambda</summary>
        lambda

        ''' <summary>lambda_arg</summary>
        lambda_arg

        ''' <summary>lambda_argn</summary>
        lambda_argn

        ''' <summary>lambda_args</summary>
        lambda_args

        ''' <summary>lambda_func</summary>
        lambda_func

        ''' <summary>let</summary>
        let_1

        ''' <summary>line</summary>
        line

        ''' <summary>list</summary>
        list

        ''' <summary>listn</summary>
        listn

        ''' <summary>namespace</summary>
        [namespace]

        ''' <summary>null</summary>
        null_1

        ''' <summary>num</summary>
        num_1

        ''' <summary>program_begin</summary>
        program_begin

        ''' <summary>start</summary>
        start

        ''' <summary>stmt</summary>
        stmt

        ''' <summary>str</summary>
        str_1

        ''' <summary>struct</summary>
        struct_1

        ''' <summary>struct_begin</summary>
        struct_begin

        ''' <summary>struct_block</summary>
        struct_block

        ''' <summary>sub</summary>
        sub_1

        ''' <summary>type</summary>
        type

        ''' <summary>typen</summary>
        typen

        ''' <summary>types</summary>
        types

        ''' <summary>typex</summary>
        typex

        ''' <summary>use</summary>
        use_1

        ''' <summary>uses</summary>
        uses

        ''' <summary>var</summary>
        var_1

        ''' <summary>varx</summary>
        varx

        ''' <summary>void</summary>
        void

        ''' <summary>$ACCEPT</summary>
        _ACCEPT

    End Enum

End Namespace
