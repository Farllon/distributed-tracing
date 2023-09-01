import pika
import psycopg2
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
import os
from time import sleep
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.resources import SERVICE_NAME, Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.instrumentation.pika import PikaInstrumentor
from opentelemetry.instrumentation.psycopg2 import Psycopg2Instrumentor

resource = Resource(attributes={
    SERVICE_NAME: "NewsletterConsumer"
})

provider = TracerProvider(resource=resource)
processor = BatchSpanProcessor(OTLPSpanExporter(endpoint=f"{os.environ['TEMPO_URL']}"))
provider.add_span_processor(processor)
trace.set_tracer_provider(provider)

Psycopg2Instrumentor().instrument()
pika_instrumentator = PikaInstrumentor()

queue_name = 'newsletters.post-created'

db_params = {
    'user': os.environ['POSTGRES_USER'],
    'password': os.environ['POSTGRES_PASSWORD'],
    'host': os.environ['POSTGRES_HOST'],
    'dbname': os.environ['POSTGRES_DB'],
    'port': os.environ['POSTGRES_PORT']
}

smtp_server = os.environ['SMTP_SERVER']
smtp_port = os.environ['SMTP_PORT']
smtp_username = os.environ['SMTP_USERNAME']
smtp_password = os.environ['SMTP_PASSWORD']
sender_email = os.environ['SMTP_SENDER']

def send_email(to_email, subject, body):
    msg = MIMEMultipart()
    msg['From'] = sender_email
    msg['To'] = to_email
    msg['Subject'] = subject
    msg.attach(MIMEText(body, 'plain'))

    smtp = smtplib.SMTP(smtp_server, smtp_port)
    smtp.starttls()
    smtp.login(smtp_username, smtp_password)
    smtp.sendmail(sender_email, to_email, msg.as_string())
    smtp.quit()

def callback(ch, method, properties, body):
    print('Mensagem recebida: ')
    print(body)

    try:
        conn = psycopg2.connect(**db_params)
        cursor = conn.cursor()

        cursor.execute("SELECT email FROM emails")
        emails = cursor.fetchall()

        for email in emails:
            print('Disparando email para ' + email[0])
            # send_email(email[0], "Assunto do Email", "Corpo do Email")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Erro: {str(e)}")

    ch.basic_ack(delivery_tag=method.delivery_tag)

while True:
    try:
        connection = pika.BlockingConnection(pika.ConnectionParameters(
        host=os.environ['RABBITMQ_HOST'], 
        port=os.environ['RABBITMQ_PORT']))
        break
    except:
        sleep(1)

channel = connection.channel()

pika_instrumentator.instrument_channel(channel=channel)

channel.queue_declare(queue=queue_name, durable=True)
channel.exchange_declare(os.environ['RABBITMQ_POST_CREATED_EXHANGE'], 'topic', durable=True)
channel.queue_bind(queue_name, os.environ['RABBITMQ_POST_CREATED_EXHANGE'], routing_key = '')

channel.basic_consume(queue=queue_name, on_message_callback=callback)

print('Aguardando mensagens. Para sair pressione Ctrl+C')
channel.start_consuming()