# CHANGE TRACKING
Para executar, antes é necessário rodar os scrips na pasta ScriptsSql e alterar a String de conexão em local.settings.json para a do seu banco local.

Esta function realiza a leitura incremental de dados de uma tabela específica de um banco de dados.
Toda lógica de leitura das alterações  está na PROC USP_RETORNA_ALTERACOES, basta alterar os parametros e pode ser reutilizada para qualquer tabela. 
