/**
 * Space Stem Homepage Chat + Voice Widget
 * Public AI Assistant powered by Space Stem Knowledge Base RAG
 */

(() => {
  'use strict';

  // Config & State
  const STORAGE_SESSION_KEY = 'spacestem_hp_chat_session_id';
  let sessionId = sessionStorage.getItem(STORAGE_SESSION_KEY) ? parseInt(sessionStorage.getItem(STORAGE_SESSION_KEY), 10) : null;
  let isWidgetOpen = false;
  let isSending = false;
  let mediaRecorder = null;
  let audioChunks = [];
  let recordingTimer = null;
  let recordingSeconds = 0;
  let mediaStream = null;

  // DOM Elements
  const launcher = document.getElementById('chatWidgetLauncher');
  const panel = document.getElementById('chatWidgetPanel');
  const closeBtn = document.getElementById('chatCloseBtn');
  const resetBtn = document.getElementById('chatResetBtn');
  const messagesBody = document.getElementById('chatMessagesBody');
  const inputForm = document.getElementById('chatInputForm');
  const inputField = document.getElementById('chatInputField');
  const sendBtn = document.getElementById('chatSendBtn');
  const micBtn = document.getElementById('chatMicBtn');
  const voiceOverlay = document.getElementById('chatVoiceOverlay');
  const recordingTimerDisplay = document.getElementById('recordingTimerDisplay');
  const stopRecordingBtn = document.getElementById('stopRecordingBtn');
  const cancelRecordingBtn = document.getElementById('cancelRecordingBtn');
  const statusToast = document.getElementById('chatStatusToast');
  const statusToastText = document.getElementById('chatStatusToastText');
  const errorAlert = document.getElementById('chatErrorAlert');
  const errorAlertText = document.getElementById('chatErrorAlertText');
  const launcherBadge = document.getElementById('chatLauncherBadge');

  if (!launcher || !panel) {
    return; // Widget elements not on page
  }

  // --- Utility Functions ---

  function escapeHtml(str) {
    if (!str) return '';
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  function formatMessageText(text) {
    if (!text) return '';
    // Strip parenthetical/bracket citation references (e.g. "(Citation 2, doc.pdf, Page 36, Chunk 35)")
    let cleaned = text
      .replace(/\s*\((?:Citation\s*\d*|Source|Reference|See)[^)]*\)/gi, '')
      .replace(/\s*\[(?:Citation\s*\d*|Source|Reference)[^\]]*\]/gi, '')
      .replace(/\s*\([^)]*\.(?:pdf|docx|txt|doc)[^)]*\)/gi, '')
      .replace(/\s+([.,;:!?])/g, '$1')
      .trim();

    let safe = escapeHtml(cleaned);
    // Basic Markdown: **bold**
    safe = safe.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    // Basic Markdown: *italic*
    safe = safe.replace(/\*(.*?)\*/g, '<em>$1</em>');
    // Basic Markdown: [link](url)
    safe = safe.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer" style="color: #7c4bb5; text-decoration: underline; font-weight: 500;">$1</a>');
    // Newlines to <br>
    safe = safe.replace(/\n/g, '<br>');
    return safe;
  }

  function formatTime(date) {
    const d = date || new Date();
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  function scrollToBottom() {
    if (messagesBody) {
      messagesBody.scrollTop = messagesBody.scrollHeight;
    }
  }

  function showToast(text, duration = 3000) {
    if (!statusToast || !statusToastText) return;
    statusToastText.textContent = text;
    statusToast.classList.add('is-visible');
    if (duration > 0) {
      setTimeout(() => {
        statusToast.classList.remove('is-visible');
      }, duration);
    }
  }

  function hideToast() {
    if (statusToast) {
      statusToast.classList.remove('is-visible');
    }
  }

  function showError(msg) {
    if (!errorAlert || !errorAlertText) return;
    errorAlertText.textContent = msg;
    errorAlert.classList.add('is-visible');
    setTimeout(() => {
      errorAlert.classList.remove('is-visible');
    }, 5000);
  }

  // --- Widget Visibility ---

  function openWidget() {
    isWidgetOpen = true;
    panel.classList.add('is-open');
    launcher.classList.add('is-active');
    launcher.setAttribute('aria-expanded', 'true');
    launcher.setAttribute('aria-label', 'Close chat');
    panel.setAttribute('aria-hidden', 'false');
    if (launcherBadge) {
      launcherBadge.style.display = 'none';
    }
    setTimeout(() => {
      inputField?.focus();
    }, 200);
    scrollToBottom();
  }

  function closeWidget() {
    isWidgetOpen = false;
    panel.classList.remove('is-open');
    launcher.classList.remove('is-active');
    launcher.setAttribute('aria-expanded', 'false');
    launcher.setAttribute('aria-label', 'Chat with us');
    panel.setAttribute('aria-hidden', 'true');
    if (mediaRecorder && mediaRecorder.state === 'recording') {
      cancelVoiceRecording();
    }
  }

  function toggleWidget() {
    if (isWidgetOpen) {
      closeWidget();
    } else {
      openWidget();
    }
  }

  // --- Rendering Messages ---

  function appendUserMessage(text) {
    const row = document.createElement('div');
    row.className = 'chat-message-row user';

    const content = document.createElement('div');
    content.className = 'chat-message-content';

    const bubble = document.createElement('div');
    bubble.className = 'chat-bubble';
    bubble.innerHTML = formatMessageText(text);

    const time = document.createElement('div');
    time.className = 'chat-message-time';
    time.textContent = formatTime();

    content.appendChild(bubble);
    content.appendChild(time);
    row.appendChild(content);

    messagesBody.appendChild(row);
    scrollToBottom();
  }

  function showTypingIndicator() {
    const row = document.createElement('div');
    row.className = 'chat-message-row assistant typing-indicator-row';
    row.id = 'chatTypingIndicator';

    const avatar = document.createElement('div');
    avatar.className = 'chat-message-avatar';
    avatar.innerHTML = '<i class="fas fa-robot"></i>';

    const bubble = document.createElement('div');
    bubble.className = 'chat-typing-bubble';
    bubble.innerHTML = `
      <span class="chat-typing-dot"></span>
      <span class="chat-typing-dot"></span>
      <span class="chat-typing-dot"></span>
    `;

    row.appendChild(avatar);
    row.appendChild(bubble);
    messagesBody.appendChild(row);
    scrollToBottom();
  }

  function removeTypingIndicator() {
    const el = document.getElementById('chatTypingIndicator');
    if (el) {
      el.remove();
    }
  }

  function appendAssistantMessage(answer, citations = [], isFallback = false) {
    removeTypingIndicator();

    const row = document.createElement('div');
    row.className = 'chat-message-row assistant';

    const avatar = document.createElement('div');
    avatar.className = 'chat-message-avatar';
    avatar.innerHTML = '<i class="fas fa-robot"></i>';

    const content = document.createElement('div');
    content.className = 'chat-message-content';

    const bubble = document.createElement('div');
    bubble.className = 'chat-bubble';
    bubble.innerHTML = formatMessageText(answer);
    content.appendChild(bubble);

    // Citations are omitted for homepage visitors per UX requirements

    // Fallback card with handoff
    if (isFallback) {
      const fallbackCard = document.createElement('div');
      fallbackCard.className = 'chat-fallback-card';
      fallbackCard.innerHTML = `
        <div><i class="fas fa-circle-question me-1"></i> Need more details or direct assistance?</div>
        <a href="mailto:recruitment@spacestem.com?subject=Recruitment%20Inquiry" class="fallback-contact-link">
          <i class="fas fa-envelope"></i> Contact Space Stem Recruitment Team &rarr;
        </a>
      `;
      content.appendChild(fallbackCard);
    }

    const time = document.createElement('div');
    time.className = 'chat-message-time';
    time.textContent = formatTime();
    content.appendChild(time);

    row.appendChild(avatar);
    row.appendChild(content);

    messagesBody.appendChild(row);
    scrollToBottom();
  }

  // --- Sending Messages to API ---

  async function sendMessage(questionText) {
    const text = (questionText || inputField?.value || '').trim();
    if (!text || isSending) return;

    if (text.length > 1000) {
      showError('Question exceeds 1000 characters.');
      return;
    }

    isSending = true;
    if (inputField) inputField.value = '';
    if (sendBtn) sendBtn.disabled = true;

    appendUserMessage(text);
    showTypingIndicator();

    try {
      const response = await fetch('/api/homepage-chat/message', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({
          message: text,
          sessionId: sessionId
        })
      });

      if (response.status === 429) {
        removeTypingIndicator();
        showError('You are asking questions quickly. Please wait a moment.');
        appendAssistantMessage('You have reached the rate limit. Please wait a minute before asking another question.', [], true);
        return;
      }

      const data = await response.json();

      if (data && data.success) {
        if (data.sessionId) {
          sessionId = data.sessionId;
          sessionStorage.setItem(STORAGE_SESSION_KEY, sessionId.toString());
        }
        appendAssistantMessage(data.answer, data.citations, data.isFallback);
      } else {
        removeTypingIndicator();
        const err = data?.errorMessage || 'Could not retrieve an answer right now.';
        appendAssistantMessage(err, [], true);
      }
    } catch (err) {
      console.error('Chat error:', err);
      removeTypingIndicator();
      appendAssistantMessage('I could not connect to the recruitment assistant. Please check your network or try again later.', [], true);
    } finally {
      isSending = false;
      if (sendBtn) sendBtn.disabled = false;
      inputField?.focus();
    }
  }

  // --- Reset / New Chat ---

  function resetChat() {
    sessionId = null;
    sessionStorage.removeItem(STORAGE_SESSION_KEY);

    // Keep initial welcome message and suggestion chips, remove other messages
    const initialWelcome = document.getElementById('chatInitialWelcome');
    const initialChips = document.getElementById('chatInitialChips');

    messagesBody.innerHTML = '';
    if (initialWelcome) messagesBody.appendChild(initialWelcome);
    if (initialChips) messagesBody.appendChild(initialChips);

    showToast('Started a new conversation');
    inputField?.focus();
  }

  // --- Speech-to-Text Voice Recording Flow ---

  async function startVoiceRecording() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      showError('Voice input is not supported in this browser.');
      return;
    }

    try {
      mediaStream = await navigator.mediaDevices.getUserMedia({
        audio: {
          channelCount: 1,
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        }
      });
    } catch (err) {
      console.warn('Microphone permission denied or failed:', err);
      showError('Microphone permission denied. You can continue typing your question.');
      return;
    }

    try {
      audioChunks = [];
      let options = {};
      if (typeof MediaRecorder.isTypeSupported === 'function') {
        if (MediaRecorder.isTypeSupported('audio/webm;codecs=opus')) {
          options = { mimeType: 'audio/webm;codecs=opus', audioBitsPerSecond: 128000 };
        } else if (MediaRecorder.isTypeSupported('audio/webm')) {
          options = { mimeType: 'audio/webm', audioBitsPerSecond: 128000 };
        } else if (MediaRecorder.isTypeSupported('audio/mp4')) {
          options = { mimeType: 'audio/mp4', audioBitsPerSecond: 128000 };
        } else if (MediaRecorder.isTypeSupported('audio/ogg')) {
          options = { mimeType: 'audio/ogg', audioBitsPerSecond: 128000 };
        }
      }

      mediaRecorder = Object.keys(options).length > 0 ? new MediaRecorder(mediaStream, options) : new MediaRecorder(mediaStream);

      mediaRecorder.ondataavailable = (e) => {
        if (e.data && e.data.size > 0) {
          audioChunks.push(e.data);
        }
      };

      mediaRecorder.onstop = async () => {
        cleanupMediaStream();
        if (audioChunks.length === 0) {
          return;
        }

        const audioBlob = new Blob(audioChunks, { type: mediaRecorder.mimeType || 'audio/webm' });
        audioChunks = [];
        await uploadAndTranscribeAudio(audioBlob);
      };

      mediaRecorder.start();
      voiceOverlay?.classList.add('is-recording');

      // Timer
      recordingSeconds = 0;
      updateTimerDisplay();
      recordingTimer = setInterval(() => {
        recordingSeconds++;
        updateTimerDisplay();
        // Limit max recording to 60 seconds
        if (recordingSeconds >= 60) {
          stopVoiceRecording();
        }
      }, 1000);
    } catch (err) {
      console.error('Failed to start MediaRecorder:', err);
      cleanupMediaStream();
      showError('Could not start audio recorder.');
    }
  }

  function updateTimerDisplay() {
    if (!recordingTimerDisplay) return;
    const mins = Math.floor(recordingSeconds / 60);
    const secs = recordingSeconds % 60;
    recordingTimerDisplay.textContent = `${mins}:${secs < 10 ? '0' : ''}${secs}`;
  }

  function stopVoiceRecording() {
    if (recordingTimer) {
      clearInterval(recordingTimer);
      recordingTimer = null;
    }
    voiceOverlay?.classList.remove('is-recording');

    if (mediaRecorder && mediaRecorder.state === 'recording') {
      mediaRecorder.stop();
    } else {
      cleanupMediaStream();
    }
  }

  function cancelVoiceRecording() {
    if (recordingTimer) {
      clearInterval(recordingTimer);
      recordingTimer = null;
    }
    voiceOverlay?.classList.remove('is-recording');
    audioChunks = [];

    if (mediaRecorder && mediaRecorder.state === 'recording') {
      mediaRecorder.ondataavailable = null;
      mediaRecorder.onstop = null;
      mediaRecorder.stop();
    }
    cleanupMediaStream();
  }

  function cleanupMediaStream() {
    if (mediaStream) {
      mediaStream.getTracks().forEach(track => track.stop());
      mediaStream = null;
    }
  }

  async function uploadAndTranscribeAudio(audioBlob) {
    showToast('Transcribing audio...', 0);

    const formData = new FormData();
    const extension = audioBlob.type.includes('mp4') ? 'mp4' : (audioBlob.type.includes('ogg') ? 'ogg' : 'webm');
    formData.append('file', audioBlob, `voice-input.${extension}`);

    try {
      const response = await fetch('/api/homepage-chat/transcribe', {
        method: 'POST',
        headers: {
          'Accept': 'application/json'
        },
        body: formData
      });

      hideToast();

      if (response.status === 429) {
        showError('Rate limit exceeded. Please wait a moment.');
        return;
      }

      const data = await response.json();

      if (data && data.success && data.transcript) {
        const transcript = data.transcript.trim();
        if (transcript) {
          if (inputField) inputField.value = transcript;
          sendMessage(transcript);
        } else {
          showError('No speech detected in audio. Please speak clearly into the microphone.');
        }
      } else {
        showError(data?.errorMessage || 'Could not transcribe speech.');
      }
    } catch (err) {
      console.error('Transcription error:', err);
      hideToast();
      showError('Audio transcription failed. Please check connection.');
    }
  }

  // --- Event Listeners ---

  launcher.addEventListener('click', toggleWidget);
  closeBtn?.addEventListener('click', closeWidget);
  resetBtn?.addEventListener('click', resetChat);

  // Keyboard accessibility
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && isWidgetOpen) {
      closeWidget();
    }
  });

  // Input form submission
  inputForm?.addEventListener('submit', (e) => {
    e.preventDefault();
    sendMessage();
  });

  // Shift+Enter vs Enter handling
  inputField?.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  });

  // Mic Button
  micBtn?.addEventListener('click', startVoiceRecording);
  stopRecordingBtn?.addEventListener('click', stopVoiceRecording);
  cancelRecordingBtn?.addEventListener('click', cancelVoiceRecording);

  // Quick suggestion chips
  document.addEventListener('click', (e) => {
    const chip = e.target.closest('.chat-suggestion-chip');
    if (chip && chip.dataset.prompt) {
      sendMessage(chip.dataset.prompt);
    }
  });

})();
