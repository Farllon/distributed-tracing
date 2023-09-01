# Use a imagem base do Python
FROM python:3.8-slim

# Defina o diretório de trabalho no contêiner
WORKDIR /app

# Copie os requisitos para o diretório de trabalho
COPY src/newsletters/consumer/requirements.txt .

# Instale as dependências
RUN pip install --no-cache-dir -r requirements.txt

# Copie o código fonte para o contêiner
COPY src/newsletters/consumer/main.py .

# Comando para iniciar o aplicativo Flask
CMD ["python", "main.py"]