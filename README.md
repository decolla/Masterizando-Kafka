# Monitoramento em Tempo Real com Apache Kafka, ksqlDB e .NET

Este repositório contém uma Prova de Conceito (PoC) de uma arquitetura **Event-Driven** para monitoramento de sensores hídricos em tempo real. O projeto demonstra a integração completa entre uma aplicação .NET (C#) e o ecossistema Confluent (Kafka + ksqlDB), simulando um cenário de IoT industrial.

## Visão Geral da Arquitetura

O sistema opera em modelo **Full-Duplex**, onde a aplicação atua simultaneamente como produtora, consumidora e controladora da infraestrutura de dados.

1.  **Ingestão (Producer):** A aplicação gera dados simulados de sensores (Nível da Água e Corrente Elétrica) e os envia para tópicos do Kafka de forma assíncrona.
2.  **Processamento (ksqlDB):** O ksqlDB atua como motor de stream processing, realizando:
    * Filtragem de dados em tempo real (`WHERE`).
    * Agregações temporais (`WINDOW TUMBLING`).
    * Junção de fluxos de dados (`STREAM-STREAM JOIN`).
3.  **Monitoramento (Consumer):** A aplicação consome tanto os dados brutos quanto os alertas críticos gerados pelo ksqlDB.
4.  **Automação (Control Plane):** Um módulo dedicado gerencia o ciclo de vida das queries no ksqlDB via **REST API**, permitindo criar e destruir regras de negócio dinamicamente.

## Tecnologias Utilizadas

* **Linguagem:** C# (.NET 8.0)
* **Message Broker:** Apache Kafka (Confluent Platform)
* **Stream Processing:** ksqlDB
* **Infraestrutura:** Docker & Docker Compose
* **Bibliotecas:**
    * `Confluent.Kafka` (Driver oficial)
    * `Newtonsoft.Json` (Serialização)

## Pré-requisitos

* [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando.
* [.NET SDK](https://dotnet.microsoft.com/download) instalado.

## Como Executar

### 1. Subir a Infraestrutura
Na raiz do projeto, execute o Docker Compose para iniciar o Zookeeper, Broker, ksqlDB Server e CLI.

```bash
docker compose up -d
Aguarde alguns instantes até que todos os containers estejam com status healthy ou running.

2. Executar a Aplicação
Navegue até a pasta do projeto C# e execute:

Bash

dotnet run
3. Interagir com o Sistema
O console apresentará um log em tempo real das mensagens enviadas e recebidas.

Produtor: Envia dados a cada segundo.

Consumidor: Lê os tópicos INTEGRA_DADOS_NIVEL_AGUA..., ...CORRENTE... e ALERTA_PERIGO.

Controlador: Digite as opções no menu para interagir com a API do ksqlDB:

1 - Criar Filtro: Injeta uma query persistente no ksqlDB que detecta nível de água > 9000.

2 - Apagar Filtro: Remove a query e o fluxo de dados processado.

Destaques de Implementação
Multithreading: Uso de Task.Run para gerenciar I/O não-bloqueante, permitindo que a ingestão de dados e o processamento de alertas ocorram paralelamente sem latência.

Infrastructure as Code Dinâmico: A aplicação não depende de scripts SQL manuais; ela provisiona a infraestrutura de stream processing via chamadas HTTP POST para o ksqlDB.

Tratamento de Erros: Implementação robusta de try/catch para conexões de rede e validação de respostas HTTP 400 (Bad Request) para depuração de sintaxe SQL.

Estrutura do Projeto
/SensorAgua: Código fonte da aplicação C#.

Program.cs: Ponto de entrada e orquestração das Threads.

DadosSensor.cs / DadosCorrente.cs: Modelos de dados (DTOs).

docker-compose.yml: Definição da infraestrutura Kafka/ksqlDB.

Desenvolvido como parte de atividades de estágio focadas em Engenharia de Dados em Tempo Real.
