from flask import Flask, request, jsonify
import re
import psycopg2
from psycopg2 import sql
from psycopg2.extensions import ISOLATION_LEVEL_AUTOCOMMIT
import os

app = Flask(__name__)

def open_sql_connection(database):
    conn = psycopg2.connect(
            database=database,
            user=os.environ['POSTGRES_USER'],
            password=os.environ['POSTGRES_PASSWORD'],
            host=os.environ['POSTGRES_HOST'],
            port=os.environ['POSTGRES_PORT'])
    
    return conn

def start_database():
    conn = open_sql_connection('postgres')
    conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
    cur = conn.cursor()
    try:
        cur.execute("CREATE DATABASE %s", os.environ['POSTGRES_DB'])
        cur.execute("CREATE TABLE emails (id serial PRIMARY KEY, email VARCHAR ( 255 ) NOT NULL)")
    except:
        pass
    cur.close()
    conn.close()
    
def is_valid_email(email):
    pattern = r'^[\w\.-]+@[\w\.-]+\.\w+$'
    return re.match(pattern, email)

@app.route('/api/newsletters/register', methods=['POST'])
def add_email():
    data = request.get_json()

    if not data or 'email' not in data:
        return jsonify({'error': 'Missing email in request'}), 400

    email = data['email']

    if not is_valid_email(email):
        return jsonify({'error': 'Invalid email format'}), 400

    try:
        conn = open_sql_connection(os.environ['POSTGRES_DB'])
        cursor = conn.cursor()
        cursor.execute("INSERT INTO emails (email) VALUES (%s);", (email,))
        conn.commit()
        cursor.close()
        conn.close()

        return None, 204

    except Exception as e:
        return jsonify({'error': 'Error saving email to database'}), 500

if __name__ == '__main__':
    start_database()
    app.run(debug=True)