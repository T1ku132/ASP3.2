import React, { useState } from 'react';

function App() {
  const [isLogin, setIsLogin] = useState(true);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  
  const [token, setToken] = useState('');
  const [protectedData, setProtectedData] = useState(null);
  const [error, setError] = useState('');

  const API_URL = 'https://localhost:53871/api'; 

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    
    const endpoint = isLogin ? '/auth/login' : '/auth/register';
    const body = isLogin ? { username, password } : { username, email, password };

    try {
      const response = await fetch(`${API_URL}${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });

      if (!response.ok) {
        if (response.status === 429) throw new Error('Слишком много попыток! Подождите минуту (Rate Limit 429).');
        if (response.status === 401) throw new Error('Неверный логин или пароль (401).');
        if (response.status === 409) throw new Error('Пользователь с таким логином или email уже существует (409).');
        
        const errorData = await response.json();
        throw new Error(errorData.error || 'Ошибка сервера');
      }

      const data = await response.json();
      setToken(data.accessToken);
    } catch (err) {
      if (err.name === 'TypeError') {
        setError('Ошибка сети! Вероятно, сработала защита CORS (сервер не разрешил ваш origin).');
      } else {
        setError(err.message);
      }
    }
  };

  const fetchProtectedData = async () => {
    setError('');
    try {
      const response = await fetch(`${API_URL}/users/1`, {
        method: 'GET',
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (!response.ok) {
        if (response.status === 403) throw new Error('Доступ запрещён (403). Вы не владелец ресурса или не Admin.');
        if (response.status === 401) throw new Error('Токен недействителен или истёк (401).');
        throw new Error('Ошибка запроса');
      }

      const data = await response.json();
      setProtectedData(data);
    } catch (err) {
      if (err.name === 'TypeError') {
        setError('Ошибка сети! Вероятно, сработала защита CORS (сервер не разрешил ваш origin).');
      } else {
        setError(err.message);
      }
    }
  };

  const handleLogout = () => {
    setToken('');
    setProtectedData(null);
    setUsername('');
    setPassword('');
    setEmail('');
    setError('');
  };

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial' }}>
      <h2>React Frontend (CORS & Auth Test)</h2>
      
      {!token ? (
        <div style={{ width: '300px' }}>
          <h3>{isLogin ? 'Вход в систему' : 'Регистрация'}</h3>
          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <input 
              type="text" 
              placeholder="Логин (Username)" 
              value={username} 
              onChange={e => setUsername(e.target.value)} 
              required 
            />
            
            {!isLogin && (
              <input 
                type="email" 
                placeholder="Email" 
                value={email} 
                onChange={e => setEmail(e.target.value)} 
                required 
              />
            )}
            
            <input 
              type="password" 
              placeholder="Пароль" 
              value={password} 
              onChange={e => setPassword(e.target.value)} 
              required 
            />
            
            <button type="submit" style={{ padding: '10px', cursor: 'pointer' }}>
              {isLogin ? 'Войти' : 'Зарегистрироваться'}
            </button>
          </form>
          
          <p style={{ marginTop: '15px', cursor: 'pointer', color: 'blue', textDecoration: 'underline' }} 
             onClick={() => { setIsLogin(!isLogin); setError(''); }}>
            {isLogin ? 'Нет аккаунта? Зарегистрироваться' : 'Уже есть аккаунт? Войти'}
          </p>
        </div>
      ) : (
        <div>
          <p style={{ color: 'green' }}><b>Успешный вход! Токен получен.</b></p>
          <p style={{ wordBreak: 'break-all', fontSize: '12px' }}>JWT: {token.substring(0, 30)}...</p>
          <button onClick={fetchProtectedData} style={{ marginRight: '10px' }}>Запросить защищенные данные (GET /users/1)</button>
          <button onClick={handleLogout}>Выйти</button>
        </div>
      )}

      {error && <p style={{ color: 'red', marginTop: '20px', fontWeight: 'bold' }}>Ошибка: {error}</p>}

      {protectedData && (
        <div style={{ marginTop: '20px', background: '#eee', padding: '10px' }}>
          <h3>Данные получены (200 OK):</h3>
          <pre>{JSON.stringify(protectedData, null, 2)}</pre>
        </div>
      )}
    </div>
  );
}

export default App;