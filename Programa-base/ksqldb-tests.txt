CREATE STREAM monitoramento_guarapiranga (
  NivelAgua INT,
  Botao BOOLEAN,
  TimeStep VARCHAR
) WITH (
  KAFKA_TOPIC = 'INTEGRA_DADOS_NIVEL_AGUA_GUARAPIRANGA_PT1',
  VALUE_FORMAT = 'JSON'


SELECT NivelAgua, Botao
FROM monitoramento_guarapiranga
WHERE NivelAgua > 850 OR ( Botao =  true AND NivelAgua < 2000)
EMIT CHANGES
