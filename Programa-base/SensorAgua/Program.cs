using Confluent.Kafka;
using Newtonsoft.Json;

namespace SensorAgua;

class DadosSensor
{
    // propiedades de um JSON
    public int NivelAgua {  get; set; }
    public bool Botao {  get; set; }
    public DateTime TimeStamp {  get; set; }

}

class Program
{
    public static async Task Main(string[] args)
    { 
        Console.WriteLine("Iniciando Aplicação...");
        
        // Executa as funções em paralelo
        var tarefaProdutor = Task.Run(() => RodarProdutor());
        var tarefaConsumidor = Task.Run(() => RodarConsumidor());
        
        // O Main espera quando as duas terminarem (o que nunca vai acontecer)
        await Task.WhenAll(tarefaProdutor, tarefaConsumidor);
    }
    
    
    // THREADS

    public static async Task RodarProdutor()
    {
        // definir o endereço do servidor/broker 
        var config = new ProducerConfig{ BootstrapServers = "localhost:9092" };
        string nomeTopico = "INTEGRA_DADOS_NIVEL_AGUA_GUARAPIRANGA_PT1";

        // using -> loop infinito que garante que a conexão feche se der erro
        // construindo um produtor com ProducerBuilder
        using (var producer = new ProducerBuilder<Null, string>(config).Build())
        {
            // inicializador aleatório 
            var random = new Random();

            while (true)
            {
                // inicializar um novo dado
                var dados = new DadosSensor
                {
                    NivelAgua = random.Next(1000, 9999),
                    Botao = false,
                    TimeStamp = DateTime.UtcNow // data e hora atual
                };

                // transformar em JSON
                string valorJson = JsonConvert.SerializeObject(dados);

                // kafka espera um objeto do tipo Message
                var mensagem = new Message<Null, string> { Value = valorJson };

                // adiciona o dado no kafka Stream
                // o await é crucial para ser assíncrono
                await producer.ProduceAsync(nomeTopico, mensagem);

                Console.WriteLine($"Enviado: {valorJson}");

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
        
        string nomeTopico = "INTEGRA_DADOS_NIVEL_AGUA_GUARAPIRANGA_PT1";

        using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
        {
            // topico para escutar
            consumer.Subscribe(nomeTopico);

            while (true)
            {
                try
                {
                    // trava a thread até chegar uma mensagem nova
                    var resultado = consumer.Consume();
                    Console.WriteLine($"<-- Recebido: {resultado.Message.Value}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Erro ao ler: {e.Message}");
                }
            }
        }
        
    }
}
