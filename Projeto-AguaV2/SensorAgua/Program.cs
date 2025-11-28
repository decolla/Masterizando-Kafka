using Confluent.Kafka;
using Newtonsoft.Json;

namespace SensorAgua;

class DadosSensor
{
    // propiedades de um JSON
    public string SensorId { get; set; }
    public int NivelAgua {  get; set; }
    public bool Botao {  get; set; }
    public DateTime TimeStamp {  get; set; }

}

class DadosCorrente
{
    public string SensorId { get; set; }
    public int ValorCorrente {  get; set; }
}

class Program
{
    public static async Task Main(string[] args)
    { 
        Console.WriteLine("Iniciando Aplicação...");
        
        // Executa as funções em paralelo
        var tarefaProdutor = Task.Run(() => RodarProdutor());
        var tarefaConsumidor = Task.Run(() => RodarConsumidor());
        var tarefaControlador = Task.Run(() => RodarControlador());
        
        // O Main espera quando as duas terminarem (o que nunca vai acontecer)
        await Task.WhenAll(tarefaProdutor, tarefaConsumidor, tarefaControlador);
    }
    
    
    // THREADS

    public static async Task RodarProdutor()
    {
        // definir o endereço do servidor/broker 
        // configurações simples de um produtor
        var config = new ProducerConfig{ BootstrapServers = "localhost:9092" };
        string nomeTopicoSensor = "INTEGRA_DADOS_NIVEL_AGUA_GUARAPIRANGA_PT1";
        string nomeTopicoCorrente = "INTEGRA_DADOS_CORRENTE_PT1";

        // using -> loop infinito que garante que a conexão feche se der erro
        // construindo um produtor com ProducerBuilder
        using (var producer = new ProducerBuilder<Null, string>(config).Build())
        {
            // inicializador aleatório 
            var random = new Random();

            while (true)
            {
                // inicializar um novo dado
                var dadosSensor = new DadosSensor
                {
                    SensorId = "sensor-01",
                    NivelAgua = random.Next(1000, 9999),
                    Botao = false,
                    TimeStamp = DateTime.UtcNow // data e hora atual
                };
                
                var dadosCorrente = new DadosCorrente
                {
                    // usar o mesmo id para o join funcionar
                    SensorId = "sensor-01",
                    ValorCorrente = random.Next(0, 50),
                };

                // transformar em JSON
                string valorJsonSensor = JsonConvert.SerializeObject(dadosSensor);
                string valorJsonCorrente = JsonConvert.SerializeObject(dadosCorrente);

                // kafka espera um objeto do tipo Message
                var mensagemSensor = new Message<Null, string> { Value = valorJsonSensor };
                var mensagemCorrente = new Message<Null, string> { Value = valorJsonCorrente };
                
                // adiciona o dado no kafka Stream
                // o await é crucial para ser assíncrono
                await producer.ProduceAsync(nomeTopicoSensor, mensagemSensor);
                await producer.ProduceAsync(nomeTopicoCorrente, mensagemCorrente);
                
                //Console.WriteLine($"Enviado Sensor: {valorJsonSensor}");
                //Console.WriteLine($"Enviado Corrente: {valorJsonCorrente}");

                // espera um pouco 
                await Task.Delay(1000);
            }
        }
    }

    public static void RodarConsumidor()
    {   
        // configurações do consumidor 
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092", // servidor de acesso
            GroupId = "integration-consumer-group", // identidade do grupo 
            AutoOffsetReset = AutoOffsetReset.Earliest // ler desde o começo se não souber de onde parou
        };
        
        string nomeTopicoSensor = "INTEGRA_DADOS_NIVEL_AGUA_GUARAPIRANGA_PT1";
        string nomeTopicoCorrente = "INTEGRA_DADOS_CORRENTE_PT1";
        // ksqlDB cria o tópico com o mesmo nome da Stream
        string nomeTopicoAlerta = "ALERTA_PERIGO";

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            // subscribe nos dois tópicos ao mesmo tempo usando uma Lista
            consumer.Subscribe(new List<string> { nomeTopicoSensor, nomeTopicoCorrente, nomeTopicoAlerta });
            
            while (true)
            {
                try
                {
                    var resultado = consumer.Consume();
                    
                    // verifica de qual tópico veio
                    if (resultado.Topic == nomeTopicoSensor)
                    {
                        //Console.WriteLine($"[Água] Recebido: {resultado.Message.Value}");
                    }
                    else if (resultado.Topic == nomeTopicoCorrente)
                    {
                        //Console.WriteLine($"[Corrente] Recebido: {resultado.Message.Value}");
                    }else if (resultado.Topic == nomeTopicoAlerta)
                    {
                        //Console.WriteLine($"[!!! PERIGO !!!] O ksqlDB detectou: {resultado.Message.Value}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Erro ao ler: {e.Message}");
                }
            }
        }
        
    }

    public static async Task RodarControlador()
    {
        // cliente http para falar com o ksqlDB
        using var client = new HttpClient();
        // define 
        client.BaseAddress = new Uri("http://localhost:8088");

        while (true)
        {
            Console.WriteLine("1 - Criar Filtro | 2 - Apagar Filtro");
            var opcao = Console.ReadLine();

            string sqlCommand = "";
            if (opcao == "1")
            {
                sqlCommand =
                    "CREATE STREAM ALERTA_PERIGO " +
                    "AS SELECT * " +
                    "FROM monitoramento_agua " +
                    "WHERE NivelAgua > 9000;";
            }
            else if (opcao == "2")
            {
                sqlCommand = "DROP STREAM ALERTA_PERIGO DELETE TOPIC;";
            }

            // se o input foi valido
            if (string.IsNullOrEmpty(sqlCommand)) continue;
            
            // monta o JSON
            string jsonCommand = JsonConvert.SerializeObject(new
            {
                ksql = sqlCommand,
                streamsProperties = new {}
            });

            // cria o conteúdo http 
            var content = new StringContent(jsonCommand, System.Text.Encoding.UTF8, "application/json");

            // mandar
            try
            {
                var response = await client.PostAsync("/ksql", content);
                
                // repsosta de sucesso ou erro
                string respostaTexto = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Resposta: {respostaTexto}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }
    }

}
