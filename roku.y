
%{
Imports Roku.Node
%}

%default INode
%define YYROOTNAMESPACE Roku
%define YYNAMESPACE     Compiler

%type<LetNode>          let
%type<IEvaluableNode>   expr
%type<IEvaluableNode>   call
%type<ListNode>         list
%type<VariableNode>     var varx
%type<NumericNode>      num
%type<StringNode>       str

%left  OPE
%right '(' '['

%%

program : void
        | program stmt


########## statement ##########
stmt : line
     | sub
     | block

block : BEGIN program END


########## expr ##########
line : expr EOL
     | let  EOL

expr : var
     | str
     | num
     | call
     | '(' expr ')'      {$$ = Me.CreateExpressionNode($2, "()")}
#     | OPE expr          {$$ = Me.CreateExpressionNode($2, $1.Name)}
     | expr OPE expr     {$$ = Me.CreateExpressionNode($1, $2.Name, $3)}
     | expr '[' expr ']' {$$ = Me.CreateExpressionNode($1, "[]", $3)}

call : expr list         {$$ = New FunctionCallNode($1, $2.List.ToArray)}

list : expr              {$$ = Me.CreateListNode($1)}
     | list expr         {$1.List.Add($2) : $$ = $1}


########## let ##########
let : LET var EQ expr    {$$ = Me.CreateLetNode($2, $4)}


########## sub ##########
sub   : SUB var '(' args ')' type EOL block
args  : void
      | argn
argn  : decla
      | argn decla
decla : var ':' type
type  : var


########## other ##########
var   : VAR     {$$ = Me.CreateVariableNode($1)}
varx  : var
      | SUB     {$$ = Me.CreateVariableNode($1)}
      | LET     {$$ = Me.CreateVariableNode($1)}
num   : NUM     {$$ = $1}
str   | STR     {$$ = New StringNode($1)}
      | str STR {$1.Append($2.Name) : $$ = $1}

void : {$$ = Nothing}

