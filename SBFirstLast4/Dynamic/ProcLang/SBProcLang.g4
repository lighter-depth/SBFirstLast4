grammar SBProcLang;

script: statement* EOF ;

statement: assignment 
         | wideAssignment 
         | internalAssignment
         | variableDeletion
         | print 
         | if_else_stat 
         | while_stat 
         | for_stat
         | break_stat
         | continue_stat
         | redo_stat
         | return_stat
         | expr_stat
         ;

stat_block: '{' statement* '}' | statement ;

if_else_stat: 'if' '(' expr ')' stat_block ('else' 'if' '(' expr ')' stat_block)* ('else' stat_block)? ;

while_stat: 'while' '(' expr ')' stat_block ;

for_stat: 'for' '(' (wideAssignment | internalAssignment | expr)? ';' expr? ';' (wideAssignment | internalAssignment | expr)? ')' stat_block ;

break_stat: 'break' ';' ;

continue_stat: 'continue' ';' ;

redo_stat: 'redo' ';' ;

return_stat: 'return' expr ';' ;

assignment: ID '=' expr ';' ;

wideAssignment: WideID ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '^' | '<<' | '>>' | '??' )? '=' expr ';' ;

internalAssignment: InternalID ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '^' | '<<' | '>>' | '??' )? '=' expr ';' ;

variableDeletion: 'delete' (WideID | InternalID) ';' ;

print: 'print' '(' expr ')' ';' ;

expr_stat: expr ';' ;


expr :  <assoc=right> expr '?' expr ':' expr
     | term ( ( '+' | '-' | Compare | Bitwise | ) term )* 
     | op=('-' | '!' | '++' | '--') expr
     | WideID
     | InternalID
     | arrayInitializer
     | hashInitializer
     | BooleanLiteral
     | NullLiteral
     | CharLiteral 
     | StringLiteral 
     | WordTypeLiteral
     | WordLiteral
     | listMarker
     | objectAccess
     | arrayAccess
     | lambda
     ;

term : factor ( ( '*' | '/' | '%' | '??' ) factor )* 
     | (WideID | InternalID) op=('++' | '--')
     | methodCall
     | inputCall
     ;

factor : Number 
       | ID 
       | WideID
       | InternalID
       | BooleanLiteral 
       | WordTypeLiteral 
       | WordLiteral
       | CharLiteral 
       | StringLiteral 
       | arrayInitializer 
       | hashInitializer 
       | listMarker 
       | NullLiteral
       |'(' expr ')' 
       ;

inputCall : 'input' '(' ')' ;

methodCall : ( ID | MethodAlias ) '(' ( expr ( ',' expr )* )? ')' ;

Number : ( '0' .. '9' )+ ( '.' ( '0' .. '9' )+ )? ;

CharLiteral : '\'' ( ~['] | EscapeSequence ) '\'' ;

StringLiteral : '"' ( ~["\n\r] | EscapeSequence )* '"' ;

EscapeSequence : '\\' [btnfr"'\\] ;

listMarker : '@' ( 'NN' | 'NW' | 'TN' | 'TW' | 'PN' | 'PW' | '8X' | '6X' | 'D4' | '4X' | 'SO' ) ;

pair : factor '=' expr ;

BooleanLiteral : 'true' | 'false' ;

WordTypeLiteral : '$' [A-Z][a-z]* ;

WordLiteral : '/' ([ぁ-ゟ] | 'ー')+ ([a-zA-Z]*)? '/' ('w' | 'd') ;

NullLiteral : 'null' ;

Compare : '==' | '!=' | '<' | '<=' | '>' | '>=' ;

Bitwise : '&' | '|' | '<<' | '>>' ;

lambda : ('(' ID ')' | ID) '=>' expr ;

arrayInitializer : '{' ( expr ( ',' expr )* )? '}' ;

hashInitializer : '%' '{' ( pair ( ',' pair )* )? '}' ;

ID: [a-zA-Z_][a-zA-Z0-9_]* ;

WideID: '&' ID ;

InternalID: '^' ID ;

MethodAlias : '\\' ( ID | '$' | '~' ) ;

objectAccess : factor '.' (ID | methodCall) objectAccessRest ;

objectAccessRest : '.' (ID | methodCall) objectAccessRest | ;

arrayAccess : factor '[' expr ']' arrayAccessRest ;

arrayAccessRest : '[' expr ']' arrayAccessRest | ;

WS: [ \t\r\n]+ -> skip;
Comment : '//' ~[\r\n]* -> skip ;
