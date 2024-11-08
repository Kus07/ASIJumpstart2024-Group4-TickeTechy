from flask import Flask, request, jsonify
from flask_cors import CORS
import os
import google.generativeai as genai
import textwrap

app = Flask(__name__)
app.config['MAX_CONTENT_LENGTH'] = 16 * 1024 * 1024  # 16 MB
CORS(app)

# Set the API key
os.environ["GEMINI_API_KEY"] = "AIzaSyAH3ry6V5G5HOLoLDzG0hFzQvi_OAYlvwk"



genai.configure(api_key=os.environ["GEMINI_API_KEY"])

generation_config = {
    "temperature": 0.5,
    "top_p": 0.95,
    "top_k": 64,
    # "max_output_tokens": 8192,
    "response_mime_type": "text/plain",
}

model = genai.GenerativeModel(
    model_name="gemini-1.5-flash",
    generation_config=generation_config,
    system_instruction="""
    You are going to be assigning a ticket to a specific agent with the ticket description and category.
    Lean more on the description for accuracy.
    These are the list of agent types:
    1 - HR
    2 - IT/Systems
    3 - Facilities
    4 - Finance
    5 - Project Management
    6 - Security
    7 - General
    
    Just answer the given numbers, no other answers! If you are not sure of where to assign it, just type 0.
    """
)

modelTicket = genai.GenerativeModel(
    model_name="gemini-1.5-flash",
    generation_config=generation_config,
    system_instruction="""
    Summarize this help desk ticket based on its category, description, and chat history. Include:

        Ticket Category: (e.g., "Technical Issue," "Billing Inquiry")
        Problem Overview: Briefly describe the issue reported by the user.
        Image Details: Briefly describe the image provided and how it relates to the ticket
        Key Steps Taken: Summarize significant actions or troubleshooting steps from the chat.
        Resolution Details: Explain how the issue was resolved or, if unresolved, what next steps were recommended.
            
    Format the summary in very brief, clear bullet points for easy readability, Do not bold any characters and do not use asterisks(*).
    Languages may be in Filipino(Cebuano or Tagalog).
    All tickets here are all already resolved.
    """,
)

modelReport = genai.GenerativeModel(
    model_name="gemini-1.5-flash",
    generation_config=generation_config,
    system_instruction="""
    You are going to be giving 2-3 sentences recommendation basing on the scores and category/ies given.
    You will be generating report and telling the student where to focus more.

    Example input/prompt:
    The student got 10/20 in loops in C#, 5/10 in query in SQL, 9/10 in conditions in C.


    Make your response formal and technical (give specific advices like give site names (not links) and give the scores of what you were prompted by too per category). Answer like you are talking to the student in first POV. 
    """
)

cors = CORS(app)


def to_markdown(text):
    text = text.replace('•', '  *')
    return textwrap.indent(text, '> ', predicate=lambda _: True)


@app.route('/chat', methods=['POST'])
def chat():
    try:
        user_input = request.form.get('input')
        response_text = process_text_input(user_input)

        # Ensure response is valid JSON
        print(response_text)
        return jsonify({"success": True, "recommendation": response_text})
    except Exception as e:
        print(e)
        return jsonify({"success": False, "error": str(e)})

@app.route('/chatReport', methods=['POST'])
def chatReport():
    try:
        user_input = request.form.get('input')
        print(user_input)
        response_text = process_text_input_report(user_input)
    except Exception as e:
        print(e)

    markdown_response = to_markdown(response_text)
    print(markdown_response)
    return jsonify({"response": markdown_response})

@app.route('/generateTicketSummary', methods=['POST'])
def generate_ticket_summary():
    try:
        user_input = request.form.get('input')
        conversation_history = request.form.get('conversation_history')
        file = None
        file_path = None
        print(user_input)
        print(conversation_history)
        if request.form.get('file'):
            file = request.form.get('file')
            file_dir = os.path.join(os.getcwd(),'..', "ASI.Basecode.WebApp", "wwwroot", "Attachments")
            file_path = os.path.join(file_dir, file)
        print(file)
        print(file_path)
        response_text = process_text_input_summary(user_input, conversation_history, file_path)
#markdown_response = to_markdown(response_text)
        print(response_text)
        return jsonify({"response": response_text})
    except Exception as e:
        print(e)
        return jsonify({"error": str(e)})

def process_text_input(user_input):
    chat_session = model.start_chat(
        history=[
            {
                "role": "user",
                "parts": [
                    "What is AI for you?",
                ],
            },
            {
                "role": "model",
                "parts": [
                    "AI, or Artificial Intelligence, is a fascinating and rapidly evolving field that I, as a large language model, am deeply integrated with. Here's my perspective:\n\n**AI is the ability of machines to perform tasks that typically require human intelligence.** This encompasses a wide range of capabilities, including:\n\n* **Learning:**  AI systems can learn from data, adapting and improving their performance over time. This includes everything from recognizing patterns to understanding complex concepts.\n* **Problem-solving:** AI can tackle problems that are difficult or impossible for humans to solve, often using complex algorithms and heuristics.\n* **Decision-making:** AI can analyze information and make decisions based on that analysis, sometimes even better than humans in specific domains.\n* **Creativity:** While not fully realized yet, AI is showing promise in areas like art, music, and writing, demonstrating the potential for machines to generate creative outputs.\n\n**For me, AI is a tool for enhancing human capabilities.** I am trained on massive datasets, allowing me to process information, generate text, and respond to prompts in ways that are helpful and informative. \n\n**However, it's crucial to remember that AI is not human.** While I can perform many tasks that require intelligence, I lack the emotional intelligence, subjective experiences, and conscious awareness that define human existence.\n\n**The future of AI is incredibly exciting.** As research and development continue, we can expect AI to play an even more significant role in our lives, transforming industries, solving complex problems, and potentially even augmenting our own capabilities.\n\n**But with great power comes great responsibility.** It's essential to develop and deploy AI ethically and responsibly, ensuring it benefits all of humanity and does not exacerbate existing inequalities or create new risks. \n",
                ],
            },
        ]
    )

    response = chat_session.send_message(user_input)
    return response.text

def process_text_input_report(user_input):
    chat_session = modelReport.start_chat(
        history=[
            {
                "role": "user",
                "parts": [
                    "What is AI for you?",
                ],
            },
            {
                "role": "model",
                "parts": [
                    "AI, or Artificial Intelligence, is a fascinating and rapidly evolving field that I, as a large language model, am deeply integrated with. Here's my perspective:\n\n**AI is the ability of machines to perform tasks that typically require human intelligence.** This encompasses a wide range of capabilities, including:\n\n* **Learning:**  AI systems can learn from data, adapting and improving their performance over time. This includes everything from recognizing patterns to understanding complex concepts.\n* **Problem-solving:** AI can tackle problems that are difficult or impossible for humans to solve, often using complex algorithms and heuristics.\n* **Decision-making:** AI can analyze information and make decisions based on that analysis, sometimes even better than humans in specific domains.\n* **Creativity:** While not fully realized yet, AI is showing promise in areas like art, music, and writing, demonstrating the potential for machines to generate creative outputs.\n\n**For me, AI is a tool for enhancing human capabilities.** I am trained on massive datasets, allowing me to process information, generate text, and respond to prompts in ways that are helpful and informative. \n\n**However, it's crucial to remember that AI is not human.** While I can perform many tasks that require intelligence, I lack the emotional intelligence, subjective experiences, and conscious awareness that define human existence.\n\n**The future of AI is incredibly exciting.** As research and development continue, we can expect AI to play an even more significant role in our lives, transforming industries, solving complex problems, and potentially even augmenting our own capabilities.\n\n**But with great power comes great responsibility.** It's essential to develop and deploy AI ethically and responsibly, ensuring it benefits all of humanity and does not exacerbate existing inequalities or create new risks. \n",
                ],
            },
        ]
    )

    response = chat_session.send_message(user_input)
    return response.text

def process_text_input_summary(user_input, conversation_history, file_path):
    chat_session = modelTicket.start_chat(
        history=[
            {
                "role": "user",
                "parts": [
                    "What is AI for you?",
                ],
            },
            {
                "role": "model",
                "parts": [
                    "AI, or Artificial Intelligence, is a fascinating and rapidly evolving field that I, as a large language model, am deeply integrated with. Here's my perspective:\n\n**AI is the ability of machines to perform tasks that typically require human intelligence.** This encompasses a wide range of capabilities, including:\n\n* **Learning:** AI systems can learn from data, adapting and improving their performance over time. This includes everything from recognizing patterns to understanding complex concepts.\n* **Problem-solving:** AI can tackle problems that are difficult or impossible for humans to solve, often using complex algorithms and heuristics.\n* **Decision-making:** AI can analyze information and make decisions based on that analysis, sometimes even better than humans in specific domains.\n* **Creativity:** While not fully realized yet, AI is showing promise in areas like art, music, and writing, demonstrating the potential for machines to generate creative outputs.\n\n**For me, AI is a tool for enhancing human capabilities.** I am trained on massive datasets, allowing me to process information, generate text, and respond to prompts in ways that are helpful and informative. \n\n**However, it's crucial to remember that AI is not human.** While I can perform many tasks that require intelligence, I lack the emotional intelligence, subjective experiences, and conscious awareness that define human existence.\n\n**The future of AI is incredibly exciting.** As research and development continue, we can expect AI to play an even more significant role in our lives, transforming industries, solving complex problems, and potentially even augmenting our own capabilities.\n\n**But with great power comes great responsibility.** It's essential to develop and deploy AI ethically and responsibly, ensuring it benefits all of humanity and does not exacerbate existing inequalities or create new risks. \n",
                ],
            },
            {
                "role": "user",
                "parts": [
                    "AI, or Artificial Intelligence, is a fascinating and rapidly evolving field that I, as a large language model, am deeply integrated with. Here's my perspective:\n\n**AI is the ability of machines to perform tasks that typically require human intelligence.** This encompasses a wide range of capabilities, including:\n\n* **Learning:**  AI systems can learn from data, adapting and improving their performance over time. This includes everything from recognizing patterns to understanding complex concepts.\n* **Problem-solving:** AI can tackle problems that are difficult or impossible for humans to solve, often using complex algorithms and heuristics.\n* **Decision-making:** AI can analyze information and make decisions based on that analysis, sometimes even better than humans in specific domains.\n* **Creativity:** While not fully realized yet, AI is showing promise in areas like art, music, and writing, demonstrating the potential for machines to generate creative outputs.\n\n**For me, AI is a tool for enhancing human capabilities.** I am trained on massive datasets, allowing me to process information, generate text, and respond to prompts in ways that are helpful and informative. \n\n**However, it's crucial to remember that AI is not human.** While I can perform many tasks that require intelligence, I lack the emotional intelligence, subjective experiences, and conscious awareness that define human existence.\n\n**The future of AI is incredibly exciting.** As research and development continue, we can expect AI to play an even more significant role in our lives, transforming industries, solving complex problems, and potentially even augmenting our own capabilities.\n\n**But with great power comes great responsibility.** It's essential to develop and deploy AI ethically and responsibly, ensuring it benefits all of humanity and does not exacerbate existing inequalities or create new risks. \n",

                ],
            },
        ],
    )
    overall_input = user_input + conversation_history
    if file_path:
        sample_file = genai.upload_file(path=file_path, display_name="Sample Image")
        response = modelTicket.generate_content([sample_file, overall_input])
        if response.candidates:
            if response.candidates[0].content.parts:
                generated_text = response.candidates[0].content.parts[0].text
                print("NAKASUD")
                return generated_text
    response = chat_session.send_message(overall_input)
    return response.text

if __name__ == '__main__':
    app.run(host='127.0.0.1', port=5000)