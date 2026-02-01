// Custom Swagger UI script to add login functionality
(function() {
    'use strict';
    
    // Wait for Swagger UI to be ready
    window.addEventListener('load', function() {
        // Wait a bit for Swagger UI to fully initialize
        setTimeout(function() {
            addLoginButton();
        }, 1000);
    });
    
    function addLoginButton() {
        // Find the authorize button area
        const authBtn = document.querySelector('.btn.authorize');
        if (!authBtn) {
            // Retry if not found
            setTimeout(addLoginButton, 500);
            return;
        }
        
        // Create login container
        const loginContainer = document.createElement('div');
        loginContainer.id = 'swagger-login-container';
        loginContainer.style.cssText = 'margin: 10px 0; padding: 10px; border: 1px solid #ccc; border-radius: 4px; background: #f9f9f9;';
        
        // Create login form
        const form = document.createElement('form');
        form.id = 'swagger-login-form';
        form.style.cssText = 'display: flex; flex-direction: column; gap: 10px;';
        
        // Username input
        const usernameLabel = document.createElement('label');
        usernameLabel.textContent = 'Username:';
        usernameLabel.style.cssText = 'font-weight: bold;';
        const usernameInput = document.createElement('input');
        usernameInput.type = 'text';
        usernameInput.id = 'swagger-username';
        usernameInput.placeholder = 'Enter username';
        usernameInput.style.cssText = 'padding: 5px; border: 1px solid #ccc; border-radius: 4px;';
        
        // Password input
        const passwordLabel = document.createElement('label');
        passwordLabel.textContent = 'Password:';
        passwordLabel.style.cssText = 'font-weight: bold;';
        const passwordInput = document.createElement('input');
        passwordInput.type = 'password';
        passwordInput.id = 'swagger-password';
        passwordInput.placeholder = 'Enter password';
        passwordInput.style.cssText = 'padding: 5px; border: 1px solid #ccc; border-radius: 4px;';
        
        // Login button
        const loginBtn = document.createElement('button');
        loginBtn.type = 'button';
        loginBtn.textContent = 'Login & Auto-Authorize';
        loginBtn.style.cssText = 'padding: 8px 16px; background: #4CAF50; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: bold;';
        loginBtn.onclick = function() {
            performLogin(usernameInput.value, passwordInput.value);
        };
        
        // Status message
        const statusMsg = document.createElement('div');
        statusMsg.id = 'swagger-login-status';
        statusMsg.style.cssText = 'margin-top: 5px; font-size: 12px;';
        
        // Assemble form
        form.appendChild(usernameLabel);
        form.appendChild(usernameInput);
        form.appendChild(passwordLabel);
        form.appendChild(passwordInput);
        form.appendChild(loginBtn);
        form.appendChild(statusMsg);
        
        loginContainer.appendChild(form);
        
        // Insert before authorize button
        if (authBtn.parentNode) {
            authBtn.parentNode.insertBefore(loginContainer, authBtn);
        }
    }
    
    function performLogin(username, password) {
        const statusMsg = document.getElementById('swagger-login-status');
        statusMsg.textContent = 'Logging in...';
        statusMsg.style.color = '#666';
        
        if (!username || !password) {
            statusMsg.textContent = 'Please enter both username and password';
            statusMsg.style.color = '#d32f2f';
            return;
        }
        
        // Call login endpoint
        fetch('/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                username: username,
                password: password
            })
        })
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => {
                    throw new Error(text || 'Login failed');
                });
            }
            return response.json();
        })
        .then(data => {
            if (data.token) {
                // Set the token in Swagger UI
                setSwaggerAuthToken(data.token);
                statusMsg.textContent = '✓ Login successful! Token has been set automatically.';
                statusMsg.style.color = '#4CAF50';
                
                // Clear password field
                document.getElementById('swagger-password').value = '';
            } else {
                throw new Error('No token in response');
            }
        })
        .catch(error => {
            statusMsg.textContent = '✗ Login failed: ' + (error.message || 'Unknown error');
            statusMsg.style.color = '#d32f2f';
            console.error('Login error:', error);
        });
    }
    
    function setSwaggerAuthToken(token) {
        // Find the authorization input field
        const authInputs = document.querySelectorAll('input[type="text"][placeholder*="token"], input[type="text"][placeholder*="Token"], input[type="text"][id*="authorization"]');
        
        if (authInputs.length > 0) {
            // Set the token value
            authInputs[0].value = token;
            
            // Trigger input event to update Swagger UI
            authInputs[0].dispatchEvent(new Event('input', { bubbles: true }));
            authInputs[0].dispatchEvent(new Event('change', { bubbles: true }));
            
            // Try to find and click the authorize button
            setTimeout(function() {
                const authorizeBtn = document.querySelector('.btn.authorize');
                if (authorizeBtn) {
                    authorizeBtn.click();
                    
                    // Close the modal after a short delay
                    setTimeout(function() {
                        const closeBtn = document.querySelector('.close-modal, .btn-done, button[aria-label="Close"]');
                        if (closeBtn) {
                            closeBtn.click();
                        }
                    }, 500);
                }
            }, 100);
        } else {
            // Alternative: Try to use Swagger UI's API
            if (window.ui && window.ui.authActions) {
                window.ui.authActions.authorize({
                    Bearer: {
                        name: 'Bearer',
                        schema: {
                            type: 'http',
                            scheme: 'bearer',
                            bearerFormat: 'JWT'
                        },
                        value: token
                    }
                });
            }
        }
    }
})();
